using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        if (config.Provider != AiProvider.OllamaLocal && string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new ArgumentException("Ushbu provayder uchun API Kalit talab qilinadi.", nameof(config.ApiKey));
        }

        try
        {
            return await ExecuteProviderCallAsync(fileBytes, mimeType, fileName, config, config.ModelName, customPrompt, ct);
        }
        catch (Exception ex)
        {
            // Automatic Model Fallback on rate limits or errors
            if (AiProviderConfig.FallbackModels.TryGetValue(config.ModelName, out var fallbackModel))
            {
                try
                {
                    Console.WriteLine($"[Fallback] {config.ModelName} xatosi ({ex.Message}). {fallbackModel} modeliga o'tilmoqda...");
                    return await ExecuteProviderCallAsync(fileBytes, mimeType, fileName, config, fallbackModel, customPrompt, ct);
                }
                catch
                {
                    throw new InvalidOperationException($"AI xizmatida xatolik ({config.Provider} - {config.ModelName}): {ex.Message}", ex);
                }
            }

            throw new InvalidOperationException($"AI xizmatida xatolik ({config.Provider} - {config.ModelName}): {ex.Message}", ex);
        }
    }

    private Task<(string Markdown, int TokensConsumed)> ExecuteProviderCallAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string modelName,
        string? customPrompt,
        CancellationToken ct)
    {
        return config.Provider switch
        {
            AiProvider.GoogleGemini => CallGeminiAsync(fileBytes, mimeType, fileName, config, modelName, customPrompt, ct),
            AiProvider.GroqAI => CallOpenAiCompatibleAsync(fileBytes, mimeType, fileName, config, "https://api.groq.com/openai/v1/chat/completions", modelName, customPrompt, ct),
            AiProvider.OpenRouter => CallOpenAiCompatibleAsync(fileBytes, mimeType, fileName, config, "https://openrouter.ai/api/v1/chat/completions", modelName, customPrompt, ct),
            AiProvider.OpenAI => CallOpenAiCompatibleAsync(fileBytes, mimeType, fileName, config, "https://api.openai.com/v1/chat/completions", modelName, customPrompt, ct),
            AiProvider.AnthropicClaude => CallClaudeAsync(fileBytes, mimeType, fileName, config, modelName, customPrompt, ct),
            AiProvider.DeepSeek => CallOpenAiCompatibleAsync(fileBytes, mimeType, fileName, config, "https://api.deepseek.com/v1/chat/completions", modelName, customPrompt, ct),
            AiProvider.OllamaLocal => CallOllamaAsync(fileBytes, mimeType, fileName, config, modelName, customPrompt, ct),
            AiProvider.OllamaCloud => CallOllamaAsync(fileBytes, mimeType, fileName, config, modelName, customPrompt, ct),
            AiProvider.CustomOpenAICompatible => CallOpenAiCompatibleAsync(fileBytes, mimeType, fileName, config, config.CustomBaseUrl ?? "http://localhost:8000/v1/chat/completions", modelName, customPrompt, ct),
            _ => throw new NotSupportedException($"Provayder qo'llab-quvvatlanmaydi: {config.Provider}")
        };
    }

    private async Task<(string Markdown, int TokensConsumed)> CallGeminiAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string modelName,
        string? customPrompt,
        CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(modelName) ? "gemini-2.5-flash" : modelName;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={config.ApiKey}";

        var base64Data = Convert.ToBase64String(fileBytes);
        var systemInstruction = BuildSystemPrompt(customPrompt);
        var promptText = $"Ushbu \"{fileName}\" ({mimeType}) faylidagi barcha matnlarni (jumladan Krill va Lotin harflari: қ, ғ, ҳ, ў), sarlavhalar va jadvallarni to'liq o'qib, toza Markdown formatiga o'tkazib ber.";

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
                        new { inline_data = new { mime_type = mimeType, data = base64Data } },
                        new { text = promptText }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini API xatoligi ({response.StatusCode}): {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var text = string.Empty;
        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                contentElem.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0)
            {
                text = parts[0].GetProperty("text").GetString() ?? string.Empty;
            }
        }

        var tokens = 0;
        if (root.TryGetProperty("usageMetadata", out var usage) &&
            usage.TryGetProperty("totalTokenCount", out var totalTokens))
        {
            tokens = totalTokens.GetInt32();
        }
        else
        {
            tokens = (int)Math.Ceiling(text.Length / 3.8);
        }

        return (CleanMarkdown(text), tokens);
    }

    private async Task<(string Markdown, int TokensConsumed)> CallOpenAiCompatibleAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string endpointUrl,
        string modelName,
        string? customPrompt,
        CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(modelName) ? "llama-3.3-70b-versatile" : modelName;
        var systemInstruction = BuildSystemPrompt(customPrompt);

        var isImage = mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        object userMessageContent;

        if (isImage)
        {
            var base64 = Convert.ToBase64String(fileBytes);
            var dataUrl = $"data:{mimeType};base64,{base64}";

            userMessageContent = new object[]
            {
                new { type = "text", text = $"Ushbu \"{fileName}\" tasvirdagi barcha matnlarni (jumladan Krill va Lotin harflari: қ, ғ, ҳ, ў) toza Markdown formatida yozib ber." },
                new { type = "image_url", image_url = new { url = dataUrl } }
            };
        }
        else
        {
            userMessageContent = $"Ushbu \"{fileName}\" fayl matnini toza Markdown formatiga o'tkazib ber.";
        }

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = (object)systemInstruction },
                new { role = "user", content = userMessageContent }
            },
            temperature = 0.1
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
        if (config.Provider == AiProvider.OpenRouter)
        {
            request.Headers.Add("HTTP-Referer", "https://github.com/aka-FinGo/MarkItDown-Studio");
            request.Headers.Add("X-Title", "MarkItDown Studio");
        }
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AI API xatoligi ({config.Provider} {response.StatusCode}): {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var text = string.Empty;
        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            text = choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }

        var tokens = 0;
        if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var totalTokens))
        {
            tokens = totalTokens.GetInt32();
        }
        else
        {
            tokens = (int)Math.Ceiling(text.Length / 3.8);
        }

        return (CleanMarkdown(text), tokens);
    }

    private async Task<(string Markdown, int TokensConsumed)> CallClaudeAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string modelName,
        string? customPrompt,
        CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(modelName) ? "claude-3-5-sonnet-20241022" : modelName;
        var url = "https://api.anthropic.com/v1/messages";

        var isImage = mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
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
                new { type = "text", text = $"Ushbu \"{fileName}\" tasvirdagi barcha matnlarni (jumladan Krill va Lotin harflari: қ, ғ, ҳ, ў) toza Markdown formatida yozib ber." }
            };
        }
        else
        {
            userContent = new object[]
            {
                new { type = "text", text = $"Ushbu \"{fileName}\" fayl ma'lumotlarini toza Markdown formatiga o'tkazib ber." }
            };
        }

        var payload = new
        {
            model = model,
            max_tokens = 8192,
            system = BuildSystemPrompt(customPrompt),
            messages = new[]
            {
                new { role = "user", content = userContent }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-api-key", config.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Claude API xatoligi ({response.StatusCode}): {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var text = string.Empty;
        if (root.TryGetProperty("content", out var contentArray) && contentArray.GetArrayLength() > 0)
        {
            text = contentArray[0].GetProperty("text").GetString() ?? string.Empty;
        }

        var tokens = 0;
        if (root.TryGetProperty("usage", out var usage))
        {
            var inTokens = usage.TryGetProperty("input_tokens", out var it) ? it.GetInt32() : 0;
            var outTokens = usage.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : 0;
            tokens = inTokens + outTokens;
        }

        return (CleanMarkdown(text), tokens);
    }

    private async Task<(string Markdown, int TokensConsumed)> CallOllamaAsync(
        byte[] fileBytes,
        string mimeType,
        string fileName,
        AiProviderConfig config,
        string modelName,
        string? customPrompt,
        CancellationToken ct)
    {
        var baseUrl = config.CustomBaseUrl?.TrimEnd('/') ?? "http://localhost:11434";
        var url = $"{baseUrl}/api/generate";
        var model = string.IsNullOrWhiteSpace(modelName) ? "llama3.2-vision" : modelName;

        var isImage = mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var base64 = isImage ? Convert.ToBase64String(fileBytes) : null;

        var payload = new
        {
            model = model,
            prompt = $"Ushbu \"{fileName}\" fayl matnini toza Markdown formatida yozib ber. {BuildSystemPrompt(customPrompt)}",
            images = isImage && base64 != null ? new[] { base64 } : null,
            stream = false
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Ollama API xatoligi ({response.StatusCode}): {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var text = root.TryGetProperty("response", out var resp) ? resp.GetString() ?? string.Empty : string.Empty;
        var tokens = root.TryGetProperty("eval_count", out var evalTokens) ? evalTokens.GetInt32() : (int)Math.Ceiling(text.Length / 3.8);

        return (CleanMarkdown(text), tokens);
    }

    private static string BuildSystemPrompt(string? customPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Siz Microsoft MarkItDown tamoyillari asosida ishlovchi universal fayl konvertatsiya tizimisiz.");
        sb.AppendLine("Vazifangiz: berilgan fayldagi barcha ma'lumotlarni, matnlarni (jumladan Krill va Lotin harflari: қ, ғ, ҳ, ў), jadvallarni va audio ovozlarni toza, tushunarli, chiroyli Markdown (.md) formatiga aylantirish.");
        sb.AppendLine("Qoidalar:");
        sb.AppendLine("1. Rasm yoki skrinshot bo'lsa (OCR): Barcha ko'rinib turgan matnlar, sarlavhalar va jadvallarni aniq o'qib, tartibli Markdown ko'rinishida yozib bering.");
        sb.AppendLine("2. Jadvallar bo'lsa: Har doim toza Markdown jadvali (| Ustun 1 | Ustun 2 |) shaklida ifodalang.");
        sb.AppendLine("3. Faqat toza Markdown qaytaring.");

        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            sb.AppendLine($"Maxsus talab: {customPrompt}");
        }

        return sb.ToString().Trim();
    }

    private static string CleanMarkdown(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var cleaned = raw.Trim();
        if (cleaned.StartsWith("```markdown\n", StringComparison.OrdinalIgnoreCase) && cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(12, cleaned.Length - 15).Trim();
        }
        else if (cleaned.StartsWith("```md\n", StringComparison.OrdinalIgnoreCase) && cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(6, cleaned.Length - 9).Trim();
        }
        else if (cleaned.StartsWith("```\n", StringComparison.OrdinalIgnoreCase) && cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(4, cleaned.Length - 7).Trim();
        }

        return cleaned;
    }
}
