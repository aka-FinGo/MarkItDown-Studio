using System.Text;
using MarkItDown.Core.Ai;
using MarkItDown.Core.Models;
using MarkItDown.Core.Ocr;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MarkItDown.Core.Converters;

public class PdfConverter
{
    private readonly IUniversalAiClient _aiClient;

    public PdfConverter(IUniversalAiClient? aiClient = null)
    {
        _aiClient = aiClient ?? new UniversalAiClient();
    }

    public async Task<string> ConvertAsync(
        byte[] pdfBytes,
        string fileName,
        string? outputDirectory,
        ConversionOptions options,
        AiProviderConfig? aiConfig,
        CancellationToken ct = default)
    {
        var totalPages = 0;
        var hasSufficientText = false;
        var pageTextList = new List<(int PageNumber, string Text, List<(string Name, byte[] Data)> Images)>();

        var cleanDocName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_");
        var attachmentFolder = $"{cleanDocName}_attachments";
        var fullAttachmentPath = !string.IsNullOrEmpty(outputDirectory)
            ? Path.Combine(outputDirectory, attachmentFolder)
            : Path.Combine(Environment.CurrentDirectory, attachmentFolder);

        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            totalPages = document.NumberOfPages;

            for (var i = 1; i <= totalPages; i++)
            {
                ct.ThrowIfCancellationRequested();
                var page = document.GetPage(i);
                var pageText = ExtractFormattedPageText(page);

                var pageImages = new List<(string Name, byte[] Data)>();
                var imgIndex = 1;

                foreach (var img in page.GetImages())
                {
                    byte[]? imgBytes = null;
                    if (img.TryGetPng(out var pngBytes) && pngBytes.Length > 100)
                    {
                        imgBytes = pngBytes;
                    }
                    else if (img.RawBytes.Length > 100)
                    {
                        imgBytes = img.RawBytes.ToArray();
                    }

                    if (imgBytes != null)
                    {
                        var imgName = $"page_{i}_img_{imgIndex}.png";
                        pageImages.Add((imgName, imgBytes));
                        imgIndex++;
                    }
                }

                pageTextList.Add((i, pageText, pageImages));

                if (pageText.Length > 30)
                {
                    hasSufficientText = true;
                }
            }
        }
        catch (Exception ex)
        {
            if (options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey))
            {
                var (aiResult, _) = await _aiClient.ConvertWithAiAsync(pdfBytes, "application/pdf", fileName, aiConfig, options.CustomPrompt, ct);
                return FormatDocument(fileName, totalPages > 0 ? totalPages : 1, aiResult);
            }

            throw new InvalidOperationException($"PDF faylni o'qishda xatolik: {ex.Message}", ex);
        }

        var isScannedPdf = !hasSufficientText || pageTextList.All(p => p.Text.Length < 30);
        var hasApiKey = options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey);

        if (pageTextList.Any(p => p.Images.Count > 0))
        {
            try { Directory.CreateDirectory(fullAttachmentPath); } catch { }
        }

        var bodySb = new StringBuilder();

        // 1. Scanned PDF with Multimodal AI
        if (isScannedPdf && hasApiKey && aiConfig != null)
        {
            if (aiConfig.Provider == AiProvider.GoogleGemini)
            {
                var (aiResult, _) = await _aiClient.ConvertWithAiAsync(pdfBytes, "application/pdf", fileName, aiConfig, options.CustomPrompt, ct);
                return FormatDocument(fileName, totalPages, aiResult);
            }

            // Other AI providers: process each page image
            for (var idx = 0; idx < pageTextList.Count; idx++)
            {
                var (pageNum, _, images) = pageTextList[idx];
                bodySb.AppendLine($"## Sahifa {pageNum}");
                bodySb.AppendLine();

                if (images.Count > 0)
                {
                    var (imgName, imgData) = images[0];
                    var relativeImgPath = $"{attachmentFolder}/{imgName}";
                    var savePath = Path.Combine(fullAttachmentPath, imgName);
                    try { await File.WriteAllBytesAsync(savePath, imgData, ct); } catch { }

                    bodySb.AppendLine($"![{imgName}]({relativeImgPath})");
                    bodySb.AppendLine();

                    var (pageMd, _) = await _aiClient.ConvertWithAiAsync(imgData, "image/png", imgName, aiConfig, options.CustomPrompt, ct);
                    bodySb.AppendLine($"> 🤖 **[AI OCR / Tasvir Tahlili]** *(Ushbu qism `{aiConfig.Provider}` - `{aiConfig.ModelName}` modeli yordamida tayyorlandi, tekshirib ko'ring)*:\n>\n" + IndentQuote(pageMd));
                }
                else
                {
                    bodySb.AppendLine("*(Ushbu sahifada o'qiladigan matn yoki tasvir topilmadi)*");
                }

                bodySb.AppendLine();
                if (idx < pageTextList.Count - 1)
                {
                    bodySb.AppendLine("---");
                    bodySb.AppendLine();
                }
            }

            return FormatDocument(fileName, totalPages, bodySb.ToString());
        }

        // 2. Normal PDF or Scanned with Windows Native Offline OCR Fallback
        for (var idx = 0; idx < pageTextList.Count; idx++)
        {
            var (pageNum, text, images) = pageTextList[idx];

            if (totalPages > 1)
            {
                bodySb.AppendLine($"## Sahifa {pageNum}");
                bodySb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                bodySb.AppendLine(text);
                bodySb.AppendLine();
            }
            else if (images.Count == 0)
            {
                bodySb.AppendLine("*(Ushbu sahifada matn qatlami topilmadi)*");
                bodySb.AppendLine();
            }

            // Embedded Images on this page (Run Windows Native OCR if no API key)
            foreach (var (imgName, imgData) in images)
            {
                var relativeImgPath = $"{attachmentFolder}/{imgName}";
                var savePath = Path.Combine(fullAttachmentPath, imgName);
                try { await File.WriteAllBytesAsync(savePath, imgData, ct); } catch { }

                bodySb.AppendLine($"![{imgName}]({relativeImgPath})");

                if (hasApiKey && aiConfig != null)
                {
                    try
                    {
                        var (ocrResult, _) = await _aiClient.ConvertWithAiAsync(imgData, "image/png", imgName, aiConfig, options.CustomPrompt, ct);
                        bodySb.AppendLine($"> 🤖 **[AI OCR / Tasvir Tahlili]** *(Ushbu qism `{aiConfig.Provider}` - `{aiConfig.ModelName}` modeli yordamida tayyorlandi, tekshirib ko'ring)*:\n>\n" + IndentQuote(ocrResult));
                    }
                    catch (Exception ex)
                    {
                        bodySb.AppendLine($"> ⚠️ *(Ushbu rasm `{relativeImgPath}` manzilida saqlandi. OCR xatosi: {ex.Message})*");
                    }
                }
                else
                {
                    // 100% Offline Windows Native OCR
                    var offlineText = await WindowsNativeOcr.RecognizeTextAsync(imgData);
                    if (!string.IsNullOrWhiteSpace(offlineText))
                    {
                        bodySb.AppendLine($"> ⚡ **[Windows Native Oflayn OCR]** *(Ushbu rasm internet va API kalitsiz, 100% oflayn OCR dvigateli orqali o'qildi)*:\n>\n" + IndentQuote(offlineText));
                    }
                    else
                    {
                        bodySb.AppendLine($"> ⚠️ *(Ushbu rasm `{relativeImgPath}` manzilida saqlandi)*");
                    }
                }
                bodySb.AppendLine();
            }

            if (totalPages > 1 && idx < pageTextList.Count - 1)
            {
                bodySb.AppendLine("---");
                bodySb.AppendLine();
            }
        }

        return FormatDocument(fileName, totalPages, bodySb.ToString());
    }

    private static string FormatDocument(string fileName, int totalPages, string bodyContent)
    {
        var sb = new StringBuilder();
        var cleanTitle = Path.GetFileNameWithoutExtension(fileName);

        sb.AppendLine($"# 📄 {cleanTitle}");
        sb.AppendLine();
        sb.AppendLine($"> 📌 **Hujjat:** `{fileName}` | **Sahifalar:** {totalPages} ta | **Format:** PDF");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(bodyContent.Trim());
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

    private static string ExtractFormattedPageText(Page page)
    {
        var sb = new StringBuilder();
        var words = page.GetWords().OrderByDescending(w => w.BoundingBox.Top).ThenBy(w => w.BoundingBox.Left).ToList();

        if (words.Count == 0)
        {
            return page.Text?.Trim() ?? string.Empty;
        }

        double? currentLineY = null;
        var currentLineWords = new List<string>();

        foreach (var word in words)
        {
            var wordY = word.BoundingBox.Bottom;

            if (currentLineY == null || Math.Abs(wordY - currentLineY.Value) > 5)
            {
                if (currentLineWords.Count > 0)
                {
                    var line = string.Join(" ", currentLineWords).Trim();
                    FormatAndAppendLine(sb, line);
                    currentLineWords.Clear();
                }
                currentLineY = wordY;
            }

            currentLineWords.Add(word.Text);
        }

        if (currentLineWords.Count > 0)
        {
            var line = string.Join(" ", currentLineWords).Trim();
            FormatAndAppendLine(sb, line);
        }

        return sb.ToString().Trim();
    }

    private static void FormatAndAppendLine(StringBuilder sb, string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        if (line.Length < 60 && line == line.ToUpperInvariant() && line.Any(char.IsLetter) && !line.EndsWith("."))
        {
            sb.AppendLine();
            sb.AppendLine($"### {line}");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine(line);
        }
    }
}
