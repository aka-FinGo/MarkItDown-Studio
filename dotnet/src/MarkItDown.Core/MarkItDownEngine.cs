using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MarkItDown.Core.Ai;
using MarkItDown.Core.Converters;
using MarkItDown.Core.Models;

namespace MarkItDown.Core;

public class MarkItDownEngine
{
    private readonly IUniversalAiClient _aiClient;
    private readonly PdfConverter _pdfConverter;
    private readonly WordConverter _wordConverter;
    private readonly ExcelConverter _excelConverter;
    private readonly PowerPointConverter _powerPointConverter;
    private readonly HtmlConverter _htmlConverter;
    private readonly CodeTextConverter _codeTextConverter;
    private readonly HttpClient _httpClient;

    public MarkItDownEngine(IUniversalAiClient? aiClient = null, HttpClient? httpClient = null)
    {
        _aiClient = aiClient ?? new UniversalAiClient();
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _pdfConverter = new PdfConverter(_aiClient);
        _wordConverter = new WordConverter();
        _excelConverter = new ExcelConverter();
        _powerPointConverter = new PowerPointConverter();
        _htmlConverter = new HtmlConverter();
        _codeTextConverter = new CodeTextConverter();
    }

    public async Task<List<ConversionResult>> ConvertFileAsync(
        string filePath,
        ConversionOptions? options = null,
        AiProviderConfig? aiConfig = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Fayl topilmadi", filePath);
        }

        var fileName = Path.GetFileName(filePath);
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        return await ConvertBytesAsync(bytes, fileName, options, aiConfig, ct);
    }

    public async Task<List<ConversionResult>> ConvertBytesAsync(
        byte[] fileBytes,
        string fileName,
        ConversionOptions? options = null,
        AiProviderConfig? aiConfig = null,
        CancellationToken ct = default)
    {
        options ??= new ConversionOptions();
        var sw = Stopwatch.StartNew();

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var results = new List<ConversionResult>();

        // Handle ZIP Archive
        if (ext == "zip")
        {
            foreach (var (entryName, data) in ZipConverter.ExtractEntries(fileBytes))
            {
                var entryResults = await ConvertBytesAsync(data, entryName, options, aiConfig, ct);
                results.AddRange(entryResults);
            }
            return results;
        }

        var markdown = string.Empty;
        var usedAi = false;
        var tokensConsumed = 0;
        var engineName = "MarkItDown .NET Local";

        var isImageOrAudio = IsImageOrAudio(ext);

        // If Image / Audio -> Direct AI Multimodal
        if (isImageOrAudio)
        {
            if (options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey))
            {
                usedAi = true;
                engineName = $"{aiConfig.Provider} Multimodal AI ({aiConfig.ModelName})";
                var mime = GetMimeType(ext);
                var aiRes = await _aiClient.ConvertWithAiAsync(fileBytes, mime, fileName, aiConfig, options.CustomPrompt, ct);
                markdown = aiRes.Markdown;
                tokensConsumed = aiRes.TokensConsumed;
            }
            else
            {
                markdown = $"> **[Tasvir/Audio Fayli]:** \"{fileName}\"\n>\n> *Ushbu formatdan matn ajratish (OCR / Transkripsiya) uchun yuqoridagi 'AI Sozlamalari' bo'limida API kalitni kiriting.*";
            }
        }
        else
        {
            try
            {
                switch (ext)
                {
                    case "pdf":
                        markdown = await _pdfConverter.ConvertAsync(fileBytes, fileName, options, aiConfig, ct);
                        break;
                    case "docx" or "doc":
                        markdown = await _wordConverter.ConvertAsync(fileBytes, ct);
                        break;
                    case "pptx" or "ppt":
                        markdown = await _powerPointConverter.ConvertAsync(fileBytes, ct);
                        break;
                    case "xlsx" or "xls" or "ods":
                        markdown = await _excelConverter.ConvertAsync(fileBytes, ct);
                        break;
                    case "csv":
                        markdown = _codeTextConverter.ConvertCsv(Encoding.UTF8.GetString(fileBytes), ",");
                        break;
                    case "tsv":
                        markdown = _codeTextConverter.ConvertCsv(Encoding.UTF8.GetString(fileBytes), "\t");
                        break;
                    case "json":
                        markdown = _codeTextConverter.ConvertJson(Encoding.UTF8.GetString(fileBytes));
                        break;
                    case "html" or "htm":
                        markdown = _htmlConverter.Convert(Encoding.UTF8.GetString(fileBytes));
                        break;
                    default:
                        var text = Encoding.UTF8.GetString(fileBytes);
                        markdown = _codeTextConverter.ConvertCode(text, ext);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey))
                {
                    usedAi = true;
                    engineName = $"{aiConfig.Provider} AI Fallback";
                    var mime = GetMimeType(ext);
                    var aiRes = await _aiClient.ConvertWithAiAsync(fileBytes, mime, fileName, aiConfig, options.CustomPrompt, ct);
                    markdown = aiRes.Markdown;
                    tokensConsumed = aiRes.TokensConsumed;
                }
                else
                {
                    throw new InvalidOperationException($"\"{fileName}\" faylini o'girishda xatolik: {ex.Message}", ex);
                }
            }
        }

        // Add YAML Frontmatter if enabled
        Dictionary<string, object>? frontmatter = null;
        if (options.IncludeFrontmatter)
        {
            var wCount = CountWords(markdown);
            var tokEst = EstimateTokens(markdown);
            frontmatter = new Dictionary<string, object>
            {
                ["sarlavha"] = Path.GetFileNameWithoutExtension(fileName),
                ["fayl_nomi"] = fileName,
                ["format"] = ext.ToUpperInvariant(),
                ["vaqt"] = DateTime.UtcNow.ToString("o"),
                ["dvigatel"] = engineName,
                ["ai_token_sarfi"] = tokensConsumed,
                ["sozlar_soni"] = wCount,
                ["taxminiy_tokenlar"] = tokEst
            };

            var yamlSb = new StringBuilder();
            yamlSb.AppendLine("---");
            foreach (var kvp in frontmatter)
            {
                yamlSb.AppendLine($"{kvp.Key}: {(kvp.Value is string s ? $"\"{s}\"" : kvp.Value)}");
            }
            yamlSb.AppendLine("---");
            yamlSb.AppendLine();
            markdown = yamlSb + markdown;
        }

        sw.Stop();

        results.Add(new ConversionResult
        {
            FileName = fileName,
            OriginalFormat = ext.ToUpperInvariant(),
            OriginalSizeBytes = fileBytes.Length,
            Markdown = markdown,
            WordCount = CountWords(markdown),
            CharCount = markdown.Length,
            LineCount = markdown.Split('\n').Length,
            EstimatedTokens = EstimateTokens(markdown),
            DurationMs = sw.ElapsedMilliseconds,
            UsedAi = usedAi,
            TokensConsumed = tokensConsumed,
            EngineName = engineName,
            Frontmatter = frontmatter,
            IsSuccess = true
        });

        return results;
    }

    public async Task<ConversionResult> ConvertUrlAsync(
        string url,
        ConversionOptions? options = null,
        AiProviderConfig? aiConfig = null,
        CancellationToken ct = default)
    {
        options ??= new ConversionOptions();
        var sw = Stopwatch.StartNew();

        var title = url;
        var markdown = string.Empty;
        var usedAi = false;
        var tokensConsumed = 0;

        try
        {
            // Try Jina Reader first
            using var jinaReq = new HttpRequestMessage(HttpMethod.Get, $"https://r.jina.ai/{url}");
            jinaReq.Headers.Add("Accept", "text/markdown");
            var jinaRes = await _httpClient.SendAsync(jinaReq, ct);

            if (jinaRes.IsSuccessStatusCode)
            {
                markdown = await jinaRes.Content.ReadAsStringAsync(ct);
                var titleMatch = Regex.Match(markdown, @"^(?:Title:|#)\s*(.*)$", RegexOptions.Multiline);
                if (titleMatch.Success)
                {
                    title = titleMatch.Groups[1].Value.Trim();
                }
            }
            else
            {
                // Fallback to direct HTML
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                var res = await _httpClient.SendAsync(req, ct);
                var html = await res.Content.ReadAsStringAsync(ct);

                var titleMatch = Regex.Match(html, @"<title[^>]*>(.*?)<\/title>", RegexOptions.IgnoreCase);
                title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : url;
                markdown = _htmlConverter.Convert(html);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Web havolani o'qib bo'lmadi: {ex.Message}", ex);
        }

        if (options.IncludeFrontmatter)
        {
            var wCount = CountWords(markdown);
            var tokEst = EstimateTokens(markdown);
            var yaml = $"---\ntitle: \"{title}\"\nsource_url: \"{url}\"\nconverted_at: \"{DateTime.UtcNow:o}\"\nword_count: {wCount}\n---\n\n";
            markdown = yaml + markdown;
        }

        sw.Stop();

        return new ConversionResult
        {
            FileName = title,
            OriginalFormat = "URL",
            OriginalSizeBytes = Encoding.UTF8.GetByteCount(markdown),
            Markdown = markdown,
            WordCount = CountWords(markdown),
            CharCount = markdown.Length,
            LineCount = markdown.Split('\n').Length,
            EstimatedTokens = EstimateTokens(markdown),
            DurationMs = sw.ElapsedMilliseconds,
            UsedAi = usedAi,
            TokensConsumed = tokensConsumed,
            SourceUrl = url,
            EngineName = "MarkItDown .NET Web Reader",
            IsSuccess = true
        };
    }

    public static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var clean = Regex.Replace(text, @"```[\s\S]*?```", "");
        clean = Regex.Replace(clean, @"[#*_`\[\]()]", " ");
        var matches = Regex.Matches(clean, @"\S+");
        return matches.Count;
    }

    public static int EstimateTokens(string text) => (int)Math.Ceiling(text.Length / 3.8);

    private static bool IsImageOrAudio(string ext)
    {
        return ext is "png" or "jpg" or "jpeg" or "webp" or "gif" or "svg" or "bmp" or "mp3" or "wav" or "m4a" or "ogg" or "flac";
    }

    private static string GetMimeType(string ext) => ext switch
    {
        "png" => "image/png",
        "jpg" or "jpeg" => "image/jpeg",
        "webp" => "image/webp",
        "gif" => "image/gif",
        "svg" => "image/svg+xml",
        "pdf" => "application/pdf",
        "mp3" => "audio/mp3",
        "wav" => "audio/wav",
        "m4a" => "audio/m4a",
        "ogg" => "audio/ogg",
        _ => "application/octet-stream"
    };
}
