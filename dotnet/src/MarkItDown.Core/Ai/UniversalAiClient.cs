using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MarkItDown.Core.Models;

namespace MarkItDown.Core.Ai;

public interface IUniversalAiClient
{
    Task<(string Markdown, int TokensConsumed)> ConvertWithAiAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string? customPrompt = null,
        CancellationToken ct = default);

    Task<(string Markdown, int TokensConsumed)> ProcessTextWithAiAsync(
        string inputContent,
        string systemInstruction,
        AiProviderConfig config,
        string? customPrompt = null,
        CancellationToken ct = default);
}

public class UniversalAiClient : IUniversalAiClient
{
    private readonly HttpClient _httpClient;

    public UniversalAiClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    public async Task<(string Markdown, int TokensConsumed)> ConvertWithAiAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string? customPrompt = null,
        CancellationToken ct = default)
    {
        return config.Provider switch
        {
            AiProvider.GoogleGemini => await CallGeminiAsync(fileBytes, mimeType, fileName, config, customPrompt, ct),
            AiProvider.OpenAI or AiProvider.DeepSeek or AiProvider.Ollama or AiProvider.CustomOpenAICompatible =>
                await CallOpenAiCompatibleAsync(fileBytes, mimeType, fileName, config, customPrompt, ct),
            AiProvider.AnthropicClaude => await CallClaudeAsync(fileBytes, mimeType, fileName, config, customPrompt, ct),
            _ => throw new NotSupportedException($"AI provayderi qo'llab-quvvatlanmaydi: {config.Provider}")
        };
    }

    public async Task<(string Markdown, int TokensConsumed)> ProcessTextWithAiAsync(
        string inputContent,
        string systemInstruction,
        AiProviderConfig config,
        string? customPrompt = null,
        CancellationToken ct = default)
    {
        var prompt = string.IsNullOrWhiteSpace(customPrompt)
            ? $"Quyidagi hujjat matnini toza, tartibli Markdown (.md) formatiga aylantirib ber:\n\n{inputContent}"
            : $"Quyidagi hujjat matnini toza Markdown formatiga aylantirib ber. Talab: {customPrompt}\n\n{inputContent}";

        if (config.Provider == AiProvider.GoogleGemini)
        {
            return await CallGeminiTextOnlyAsync(prompt, systemInstruction, config, ct);
        }

        return await CallOpenAiTextOnlyAsync(prompt, systemInstruction, config, ct);
    }

    // 1. Google Gemini Multimodal Call
    private async Task<(string Markdown, int TokensConsumed)> CallGeminiAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string? customPrompt,
        CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(config.ModelName) ? "gemini-2.5-flash" : config.ModelName;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={config.ApiKey}";

        var base64Data = Convert.ToBase64String(fileBytes);
        var systemInstruction = GetUniversalSystemPrompt(customPrompt);
        var promptText = $"Ushbu \"{fileName}\" ({mimeType}) faylidagi barcha matnlarni, jadvallarni va mazmunni to'liq ajratib, toza Markdown (.md) formatiga o'tkazib ber.";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64Data
                            }
                        },
                        new
                        {
                            text = promptText
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = config.Temperature
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini API xatoligi ({response.StatusCode}): {responseJson}");
        }

        var doc = JsonNode.Parse(responseJson);
        var rawText = doc?["candidates"]?[0]?["content"]?[parts()]?[0]?["text"]?.GetValue<string>()
                      ?? doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
                      ?? string.Empty;

        var tokens = doc?["usageMetadata"]?["totalTokenCount"]?.GetValue<int>() ?? EstimateTokens(rawText);
        return (CleanMarkdownFences(rawText), tokens);
    }

    private static string parts() => "parts";

    private async Task<(string Markdown, int TokensConsumed)> CallGeminiTextOnlyAsync(
        string prompt,
        string systemInstruction,
        AiProviderConfig config,
        CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(config.ModelName) ? "gemini-2.5-flash" : config.ModelName;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={config.ApiKey}";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = config.Temperature
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini API xatoligi ({response.StatusCode}): {responseJson}");
        }

        var doc = JsonNode.Parse(responseJson);
        var rawText = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
        var tokens = doc?["usageMetadata"]?["totalTokenCount"]?.GetValue<int>() ?? EstimateTokens(rawText);
        return (CleanMarkdownFences(rawText), tokens);
    }

    // 2. OpenAI / DeepSeek / Ollama / Custom API Call
    private async Task<(string Markdown, int TokensConsumed)> CallOpenAiCompatibleAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string? customPrompt,
        CancellationToken ct)
    {
        var baseUrl = config.Provider switch
        {
            AiProvider.DeepSeek => "https://api.deepseek.com/v1/chat/completions",
            AiProvider.Ollama => string.IsNullOrWhiteSpace(config.CustomBaseUrl) ? "http://localhost:11434/v1/chat/completions" : $"{config.CustomBaseUrl.TrimEnd('/')}/chat/completions",
            AiProvider.CustomOpenAICompatible => string.IsNullOrWhiteSpace(config.CustomBaseUrl) ? "https://api.openai.com/v1/chat/completions" : $"{config.CustomBaseUrl.TrimEnd('/')}/chat/completions",
            _ => "https://api.openai.com/v1/chat/completions"
        };

        var model = string.IsNullOrWhiteSpace(config.ModelName) ? "gpt-4o-mini" : config.ModelName;
        var systemInstruction = GetUniversalSystemPrompt(customPrompt);
        var isImage = mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        var messages = new List<object>
        {
            new { role = "system", content = systemInstruction }
        };

        if (isImage)
        {
            var base64 = Convert.ToBase64String(fileBytes);
            var dataUrl = $"data:{mimeType};base64,{base64}";
            messages.Add(new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = $"Ushbu \"{fileName}\" tasvirdagi barcha matnlarni (OCR) va ma'lumotlarni toza Markdown formatida yozib ber." },
                    new { type = "image_url", image_url = new { url = dataUrl } }
                }
            });
        }
        else
        {
            var textContent = Encoding.UTF8.GetString(fileBytes);
            messages.Add(new
            {
                role = "user",
                content = $"Ushbu \"{fileName}\" fayl matnini toza Markdown (.md) formatiga aylantirib ber:\n\n{textContent}"
            });
        }

        var payload = new
        {
            model,
            messages,
            temperature = config.Temperature
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        }
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{config.Provider} API xatoligi ({response.StatusCode}): {responseJson}");
        }

        var doc = JsonNode.Parse(responseJson);
        var rawText = doc?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
        var tokens = doc?["usage"]?["total_tokens"]?.GetValue<int>() ?? EstimateTokens(rawText);

        return (CleanMarkdownFences(rawText), tokens);
    }

    private async Task<(string Markdown, int TokensConsumed)> CallOpenAiTextOnlyAsync(
        string prompt,
        string systemInstruction,
        AiProviderConfig config,
        CancellationToken ct)
    {
        var baseUrl = config.Provider switch
        {
            AiProvider.DeepSeek => "https://api.deepseek.com/v1/chat/completions",
            AiProvider.Ollama => string.IsNullOrWhiteSpace(config.CustomBaseUrl) ? "http://localhost:11434/v1/chat/completions" : $"{config.CustomBaseUrl.TrimEnd('/')}/chat/completions",
            AiProvider.CustomOpenAICompatible => string.IsNullOrWhiteSpace(config.CustomBaseUrl) ? "https://api.openai.com/v1/chat/completions" : $"{config.CustomBaseUrl.TrimEnd('/')}/chat/completions",
            _ => "https://api.openai.com/v1/chat/completions"
        };

        var model = string.IsNullOrWhiteSpace(config.ModelName) ? "gpt-4o-mini" : config.ModelName;

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemInstruction },
                new { role = "user", content = prompt }
            },
            temperature = config.Temperature
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        }
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{config.Provider} API xatoligi ({response.StatusCode}): {responseJson}");
        }

        var doc = JsonNode.Parse(responseJson);
        var rawText = doc?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
        var tokens = doc?["usage"]?["total_tokens"]?.GetValue<int>() ?? EstimateTokens(rawText);

        return (CleanMarkdownFences(rawText), tokens);
    }

    // 3. Anthropic Claude Call
    private async Task<(string Markdown, int TokensConsumed)> CallClaudeAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string? customPrompt,
        CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(config.ModelName) ? "claude-3-5-sonnet-20241022" : config.ModelName;
        var url = "https://api.anthropic.com/v1/messages";

        var isImage = mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var systemPrompt = GetUniversalSystemPrompt(customPrompt);

        object userContent;
        if (isImage)
        {
            var base64 = Convert.ToBase64String(fileBytes);
            userContent = new object[]
            {
                new
                {
                    type = "image",
                    source = new
                    {
                        type = "base64",
                        media_type = mimeType,
                        data = base64
                    }
                },
                new
                {
                    type = "text",
                    text = $"Ushbu \"{fileName}\" tasvirdagi barcha matnlarni (OCR) toza Markdown formatida qaytaring."
                }
            };
        }
        else
        {
            userContent = $"Ushbu \"{fileName}\" faylni toza Markdown formatiga aylantiring:\n\n{Encoding.UTF8.GetString(fileBytes)}";
        }

        var payload = new
        {
            model,
            max_tokens = 8192,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userContent }
            },
            temperature = config.Temperature
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-api-key", config.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Claude API xatoligi ({response.StatusCode}): {responseJson}");
        }

        var doc = JsonNode.Parse(responseJson);
        var rawText = doc?["content"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
        var inTokens = doc?["usage"]?["input_tokens"]?.GetValue<int>() ?? 0;
        var outTokens = doc?["usage"]?["output_tokens"]?.GetValue<int>() ?? 0;

        return (CleanMarkdownFences(rawText), inTokens + outTokens);
    }

    private static string GetUniversalSystemPrompt(string? customPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Siz Microsoft MarkItDown tamoyillari asosida ishlovchi universal fayl konvertatsiya tizimisiz.");
        sb.AppendLine("Vazifangiz: berilgan fayldagi barcha ma'lumotlarni, matnlarni, jadvallarni va tuzilmalarni toza, tushunarli, chiroyli Markdown (.md) formatiga aylantirish.");
        sb.AppendLine("Qoidalar:");
        sb.AppendLine("1. Rasm yoki skrinshot bo'lsa (OCR): Tasvirdagi barcha ko'rinib turgan matnlar, sarlavhalar va jadvallarni aniq o'qib, tartibli Markdown ko'rinishida yozib bering.");
        sb.AppendLine("2. Audio fayl bo'lsa: Nutqni to'liq eshitib, matnga aylantiring (Transkripsiya).");
        sb.AppendLine("3. Jadvallar bo'lsa: Har doim toza Markdown jadvali (| Ustun 1 | Ustun 2 |) shaklida ifodalang.");
        sb.AppendLine("4. Hech qanday boshqa kirish yoki yakuniy tushuntirish so'zlari yozmang. Faqat toza Markdown matnini qaytaring.");
        sb.AppendLine("5. Chiqishni ```markdown kabi bloklarga o'ramang, to'g'ridan-to'g'ri Markdown matni bo'lsin.");

        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            sb.AppendLine($"Foydalanuvchining maxsus talabi: {customPrompt}");
        }

        return sb.ToString();
    }

    private static string CleanMarkdownFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```markdown", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith("```"))
        {
            var firstLineBreak = trimmed.IndexOf('\n');
            if (firstLineBreak > 0)
            {
                trimmed = trimmed.Substring(firstLineBreak + 1, trimmed.Length - firstLineBreak - 4);
            }
        }
        else if (trimmed.StartsWith("```md", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith("```"))
        {
            var firstLineBreak = trimmed.IndexOf('\n');
            if (firstLineBreak > 0)
            {
                trimmed = trimmed.Substring(firstLineBreak + 1, trimmed.Length - firstLineBreak - 4);
            }
        }
        else if (trimmed.StartsWith("```") && trimmed.EndsWith("```"))
        {
            var firstLineBreak = trimmed.IndexOf('\n');
            if (firstLineBreak > 0)
            {
                trimmed = trimmed.Substring(firstLineBreak + 1, trimmed.Length - firstLineBreak - 4);
            }
        }
        return trimmed.Trim();
    }

    private static int EstimateTokens(string text) => (int)Math.Ceiling(text.Length / 3.8);
}
