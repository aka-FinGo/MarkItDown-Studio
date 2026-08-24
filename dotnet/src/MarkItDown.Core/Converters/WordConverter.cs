using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MarkItDown.Core.Ai;
using MarkItDown.Core.Models;

namespace MarkItDown.Core.Converters;

public class WordConverter
{
    private readonly IUniversalAiClient _aiClient;

    public WordConverter(IUniversalAiClient? aiClient = null)
    {
        _aiClient = aiClient ?? new UniversalAiClient();
    }

    public async Task<string> ConvertAsync(
        byte[] docxBytes,
        string fileName,
        string? outputDirectory,
        ConversionOptions options,
        AiProviderConfig? aiConfig,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        var tocList = new List<string>();
        var cleanDocName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_");
        var attachmentFolder = $"{cleanDocName}_attachments";
        var fullAttachmentPath = !string.IsNullOrEmpty(outputDirectory)
            ? Path.Combine(outputDirectory, attachmentFolder)
            : Path.Combine(Environment.CurrentDirectory, attachmentFolder);

        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);

        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return string.Empty;

        var hasApiKey = options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey);
        var bodySb = new StringBuilder();
        var imgIndex = 1;

        foreach (var element in body.Elements())
        {
            ct.ThrowIfCancellationRequested();

            if (element is Paragraph p)
            {
                var style = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
                var text = ExtractParagraphText(p);

                // Check for embedded images in this paragraph
                var drawings = p.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().ToList();
                foreach (var blip in drawings)
                {
                    if (blip.Embed?.Value != null && doc.MainDocumentPart != null)
                    {
                        var imagePart = doc.MainDocumentPart.GetPartById(blip.Embed.Value);
                        using var imgStream = imagePart.GetStream();
                        using var ms = new MemoryStream();
                        imgStream.CopyTo(ms);
                        var imgData = ms.ToArray();

                        if (imgData.Length > 100)
                        {
                            try { Directory.CreateDirectory(fullAttachmentPath); } catch { }
                            var imgName = $"word_img_{imgIndex}.png";
                            var relativeImgPath = $"{attachmentFolder}/{imgName}";
                            var savePath = Path.Combine(fullAttachmentPath, imgName);
                            try { await File.WriteAllBytesAsync(savePath, imgData, ct); } catch { }

                            bodySb.AppendLine($"![{imgName}]({relativeImgPath})");
                            if (hasApiKey && aiConfig != null)
                            {
                                try
                                {
                                    var (ocrRes, _) = await _aiClient.ConvertWithAiAsync(imgData, "image/png", imgName, aiConfig, options.CustomPrompt, ct);
                                    bodySb.AppendLine($"> 🤖 **[AI OCR / Tasvir Tahlili]** *(Ushbu qism `{aiConfig.Provider}` - `{aiConfig.ModelName}` modeli yordamida tayyorlandi, tekshirib ko'ring)*:\n>\n> " + ocrRes.Replace("\n", "\n> "));
                                }
                                catch (Exception ex)
                                {
                                    bodySb.AppendLine($"> ⚠️ *(Ushbu rasm `{relativeImgPath}` manzilida saqlandi. OCR xatosi: {ex.Message})*");
                                }
                            }
                            else
                            {
                                bodySb.AppendLine($"> ⚠️ *(Ushbu rasm `{relativeImgPath}` manzilida saqlandi. AI API kaliti ulanmagani sababli rasmdagi matn ajratib olinmadi)*");
                            }
                            bodySb.AppendLine();
                            imgIndex++;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(text)) continue;

                if (style.StartsWith("Heading1", StringComparison.OrdinalIgnoreCase))
                {
                    bodySb.AppendLine($"# {text}\n");
                    tocList.Add(text);
                }
                else if (style.StartsWith("Heading2", StringComparison.OrdinalIgnoreCase))
                {
                    bodySb.AppendLine($"## {text}\n");
                    tocList.Add(text);
                }
                else if (style.StartsWith("Heading3", StringComparison.OrdinalIgnoreCase))
                {
                    bodySb.AppendLine($"### {text}\n");
                }
                else if (p.ParagraphProperties?.NumberingProperties != null)
                {
                    bodySb.AppendLine($"- {text}");
                }
                else
                {
                    bodySb.AppendLine(text);
                    bodySb.AppendLine();
                }
            }
            else if (element is Table table)
            {
                var mdTable = ConvertTableToMarkdown(table);
                if (!string.IsNullOrWhiteSpace(mdTable))
                {
                    bodySb.AppendLine(mdTable);
                    bodySb.AppendLine();
                }
            }
        }

        // Build Obsidian Output
        sb.AppendLine($"# 📄 {Path.GetFileNameWithoutExtension(fileName)}");
        sb.AppendLine();
        sb.AppendLine($"> 📌 **Hujjat:** `{fileName}` | **Format:** Word (.docx)");
        sb.AppendLine();

        if (tocList.Count > 0)
        {
            sb.AppendLine("## 📑 Mundarija");
            foreach (var h in tocList)
            {
                sb.AppendLine($"- [[#{h}|{h}]]");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine(bodySb.ToString().Trim());
        return sb.ToString().Trim();
    }

    private static string ExtractParagraphText(Paragraph p)
    {
        var sb = new StringBuilder();
        foreach (var run in p.Elements<Run>())
        {
            var text = run.InnerText;
            if (string.IsNullOrEmpty(text)) continue;

            var isBold = run.RunProperties?.Bold != null;
            var isItalic = run.RunProperties?.Italic != null;

            if (isBold && isItalic)
                sb.Append($"***{text}***");
            else if (isBold)
                sb.Append($"**{text}**");
            else if (isItalic)
                sb.Append($"*{text}*");
            else
                sb.Append(text);
        }
        return sb.ToString().Trim();
    }

    private static string ConvertTableToMarkdown(Table table)
    {
        var rows = table.Elements<TableRow>().ToList();
        if (rows.Count == 0) return string.Empty;

        var parsedRows = new List<List<string>>();

        foreach (var tr in rows)
        {
            var cells = tr.Elements<TableCell>()
                .Select(tc => tc.InnerText.Trim().Replace("|", "\\|").Replace("\r", " ").Replace("\n", " "))
                .ToList();
            parsedRows.Add(cells);
        }

        if (parsedRows.Count == 0) return string.Empty;

        var maxCols = parsedRows.Max(r => r.Count);
        if (maxCols == 0) return string.Empty;

        foreach (var row in parsedRows)
        {
            while (row.Count < maxCols) row.Add(string.Empty);
        }

        var header = parsedRows[0];
        var separator = header.Select(_ => "---").ToList();
        var dataRows = parsedRows.Skip(1).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"| {string.Join(" | ", header)} |");
        sb.AppendLine($"| {string.Join(" | ", separator)} |");

        foreach (var row in dataRows)
        {
            sb.AppendLine($"| {string.Join(" | ", row)} |");
        }

        return sb.ToString().Trim();
    }
}
