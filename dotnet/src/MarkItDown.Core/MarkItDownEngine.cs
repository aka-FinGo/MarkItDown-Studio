using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MarkItDown.Core.Ai;
using MarkItDown.Core.Converters;
using MarkItDown.Core.Models;
using MarkItDown.Core.Ocr;

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
        _wordConverter = new WordConverter(_aiClient);
        _excelConverter = new ExcelConverter();
        _powerPointConverter = new PowerPointConverter(_aiClient);
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
        var outputDirectory = Path.GetDirectoryName(filePath);
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        return await ConvertBytesAsync(bytes, fileName, outputDirectory, options, aiConfig, ct);
    }

    public async Task<List<ConversionResult>> ConvertBytesAsync(
        byte[] fileBytes,
        string fileName,
        string? outputDirectory = null,
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
                var entryResults = await ConvertBytesAsync(data, entryName, outputDirectory, options, aiConfig, ct);
                results.AddRange(entryResults);
            }
            return results;
        }

        var markdown = string.Empty;
        var usedAi = false;
        var tokensConsumed = 0;
        var engineName = "MarkItDown .NET Local";
        var isImageOrAudio = IsImageOrAudio(ext);
        var hasApiKey = options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey);

        // 1. Image / Audio Handling
        if (isImageOrAudio)
        {
            var isAudio = ext is "mp3" or "wav" or "m4a" or "ogg" or "flac";
            if (hasApiKey && aiConfig != null)
            {
                usedAi = true;
                engineName = $"{aiConfig.Provider} ({aiConfig.ModelName})";
                var mime = GetMimeType(ext);
                var aiRes = await _aiClient.ConvertWithAiAsync(fileBytes, mime, fileName, aiConfig, options.CustomPrompt, ct);
                tokensConsumed = aiRes.TokensConsumed;

                var imgSb = new StringBuilder();
                imgSb.AppendLine($"# 📄 {Path.GetFileNameWithoutExtension(fileName)}");
                imgSb.AppendLine();
                imgSb.AppendLine($"> 📌 **{(isAudio ? "Audio" : "Tasvir")}:** `{fileName}` | **Format:** {ext.ToUpperInvariant()}");
                imgSb.AppendLine();

                if (!isAudio)
                {
                    imgSb.AppendLine($"![{fileName}]({fileName})");
                    imgSb.AppendLine();
                }

                imgSb.AppendLine($"> 🤖 **[AI OCR / {(isAudio ? "Audio Transkripsiya" : "Tasvir Tahlili")}]** *(Ushbu qism `{aiConfig.Provider}` - `{aiConfig.ModelName}` modeli yordamida tayyorlandi, tekshirib ko'ring)*:\n>\n" + IndentQuote(aiRes.Markdown));
                markdown = imgSb.ToString().Trim();
            }
            else
            {
                var imgSb = new StringBuilder();
                imgSb.AppendLine($"# 📄 {Path.GetFileNameWithoutExtension(fileName)}");
                imgSb.AppendLine();
                imgSb.AppendLine($"> 📌 **{(isAudio ? "Audio" : "Tasvir")}:** `{fileName}` | **Format:** {ext.ToUpperInvariant()}");
                imgSb.AppendLine();
                if (!isAudio)
                {
                    imgSb.AppendLine($"![{fileName}]({fileName})");
                    imgSb.AppendLine();

                    // Run Windows Native Offline OCR
                    var offlineText = await WindowsNativeOcr.RecognizeTextAsync(fileBytes);
                    if (!string.IsNullOrWhiteSpace(offlineText))
                    {
                        engineName = "Windows Native Oflayn OCR";
                        imgSb.AppendLine($"> ⚡ **[Windows Native Oflayn OCR]** *(Ushbu rasm internet va API kalitsiz, 100% oflayn OCR dvigateli orqali o'qildi)*:\n>\n" + IndentQuote(offlineText));
                    }
                    else
                    {
                        imgSb.AppendLine($"> ⚠️ *(Ushbu rasm saqlandi. Matn aniqlanmadi)*");
                    }
                }
                else
                {
                    imgSb.AppendLine($"> ⚠️ *(Ushbu audio yuklandi. Ovozli transkripsiya uchun AI kalit kerak)*");
                }
                markdown = imgSb.ToString().Trim();
            }
        }
        else
        {
            // 2. Documents
            try
            {
                switch (ext)
                {
                    case "pdf":
                        markdown = await _pdfConverter.ConvertAsync(fileBytes, fileName, outputDirectory, options, aiConfig, ct);
                        break;
                    case "docx" or "doc":
                        markdown = await _wordConverter.ConvertAsync(fileBytes, fileName, outputDirectory, options, aiConfig, ct);
                        break;
                    case "pptx" or "ppt":
                        markdown = await _powerPointConverter.ConvertAsync(fileBytes, fileName, outputDirectory, options, aiConfig, ct);
                        break;
                    case "xlsx" or "xls" or "ods":
                        markdown = await _excelConverter.ConvertAsync(fileBytes, fileName, ct);
                        break;
                    case "csv":
                        markdown = FormatTextDocument(fileName, ext, _codeTextConverter.ConvertCsv(Encoding.UTF8.GetString(fileBytes), ","));
                        break;
                    case "tsv":
                        markdown = FormatTextDocument(fileName, ext, _codeTextConverter.ConvertCsv(Encoding.UTF8.GetString(fileBytes), "\t"));
                        break;
                    case "json":
                        markdown = FormatTextDocument(fileName, ext, _codeTextConverter.ConvertJson(Encoding.UTF8.GetString(fileBytes)));
                        break;
                    case "html" or "htm":
                        markdown = FormatTextDocument(fileName, ext, _htmlConverter.Convert(Encoding.UTF8.GetString(fileBytes)));
                        break;
                    default:
                        var text = Encoding.UTF8.GetString(fileBytes);
                        markdown = FormatTextDocument(fileName, ext, _codeTextConverter.ConvertCode(text, ext));
                        break;
                }
            }
            catch (Exception ex)
            {
                if (hasApiKey && aiConfig != null)
                {
                    usedAi = true;
                    engineName = $"{aiConfig.Provider} AI Fallback";
                    var mime = GetMimeType(ext);
                    var aiRes = await _aiClient.ConvertWithAiAsync(fileBytes, mime, fileName, aiConfig, options.CustomPrompt, ct);
                    markdown = FormatTextDocument(fileName, ext, aiRes.Markdown);
                    tokensConsumed = aiRes.TokensConsumed;
                }
                else
                {
                    throw new InvalidOperationException($"\"{fileName}\" faylini o'girishda xatolik: {ex.Message}", ex);
                }
            }
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

        var obsidianUrlMd = $"# 🌐 {title}\n\n> 📌 **Manba:** [{url}]({url})\n\n---\n\n{markdown.Trim()}";

        sw.Stop();

        return new ConversionResult
        {
            FileName = title,
            OriginalFormat = "URL",
            OriginalSizeBytes = Encoding.UTF8.GetByteCount(obsidianUrlMd),
            Markdown = obsidianUrlMd,
            WordCount = CountWords(obsidianUrlMd),
            CharCount = obsidianUrlMd.Length,
            LineCount = obsidianUrlMd.Split('\n').Length,
            EstimatedTokens = EstimateTokens(obsidianUrlMd),
            DurationMs = sw.ElapsedMilliseconds,
            UsedAi = usedAi,
            TokensConsumed = tokensConsumed,
            SourceUrl = url,
            EngineName = "MarkItDown .NET Web Reader",
            IsSuccess = true
        };
    }

    private static string FormatTextDocument(string fileName, string format, string body)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# 📄 {Path.GetFileNameWithoutExtension(fileName)}");
        sb.AppendLine();
        sb.AppendLine($"> 📌 **Hujjat:** `{fileName}` | **Format:** {format.ToUpperInvariant()}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(body.Trim());
        return sb.ToString().Trim();
    }

    private static string IndentQuote(string text)
    {
        var lines = text.Split('\n');
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.AppendLine($"> {line}");
        }
        return sb.ToString().TrimEnd();
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
        "bmp" => "image/bmp",
        "pdf" => "application/pdf",
        "mp3" => "audio/mp3",
        "wav" => "audio/wav",
        "m4a" => "audio/m4a",
        "ogg" => "audio/ogg",
        _ => "application/octet-stream"
    };
}
