using System.Text;
using MarkItDown.Core.Ai;
using MarkItDown.Core.Models;
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
        ConversionOptions options,
        AiProviderConfig? aiConfig,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        var totalPages = 0;
        var hasSufficientText = false;
        var pageTextList = new List<(int PageNumber, string Text)>();

        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            totalPages = document.NumberOfPages;

            for (var i = 1; i <= totalPages; i++)
            {
                var page = document.GetPage(i);
                var pageText = ExtractFormattedPageText(page);
                pageTextList.Add((i, pageText));

                if (pageText.Length > 30)
                {
                    hasSufficientText = true;
                }
            }
        }
        catch (Exception ex)
        {
            // If PDF reading fails and AI is configured, fallback to AI
            if (options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey))
            {
                var (aiResult, _) = await _aiClient.ConvertWithAiAsync(pdfBytes, "application/pdf", fileName, aiConfig, options.CustomPrompt, ct);
                return aiResult;
            }

            throw new InvalidOperationException($"PDF faylni o'qishda xatolik: {ex.Message}", ex);
        }

        // SMART SCANNED DETECTION:
        // Agar PDF skanerlangan (matn qatlami bo'sh yoki 3 ta sahifadan faqat sahifa raqamlari chiqqan) bo'lsa:
        if ((!hasSufficientText || pageTextList.All(p => p.Text.Length < 30)) && options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey))
        {
            var (aiResult, _) = await _aiClient.ConvertWithAiAsync(pdfBytes, "application/pdf", fileName, aiConfig, options.CustomPrompt, ct);
            return aiResult;
        }

        // Build clean Markdown from extracted pages
        for (var idx = 0; idx < pageTextList.Count; idx++)
        {
            var (pageNum, text) = pageTextList[idx];

            if (totalPages > 1)
            {
                sb.AppendLine($"## Sahifa {pageNum}");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("*(Ushbu sahifada matn qatlami topilmadi — skanerlangan tasvir bo'lishi mumkin)*");
                sb.AppendLine();
            }

            if (totalPages > 1 && idx < pageTextList.Count - 1)
            {
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }

        return sb.ToString().Trim();
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

        // Auto detect headings (e.g. ALL CAPS or short title)
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
