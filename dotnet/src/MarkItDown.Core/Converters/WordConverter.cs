using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MarkItDown.Core.Converters;

public class WordConverter
{
    public Task<string> ConvertAsync(byte[] docxBytes, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);

        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null)
        {
            return Task.FromResult(string.Empty);
        }

        foreach (var element in body.Elements())
        {
            ct.ThrowIfCancellationRequested();

            if (element is Paragraph p)
            {
                var style = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
                var text = ExtractParagraphText(p);

                if (string.IsNullOrWhiteSpace(text)) continue;

                if (style.StartsWith("Heading1", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"# {text}\n");
                }
                else if (style.StartsWith("Heading2", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"## {text}\n");
                }
                else if (style.StartsWith("Heading3", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"### {text}\n");
                }
                else if (style.StartsWith("Heading4", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"#### {text}\n");
                }
                else if (p.ParagraphProperties?.NumberingProperties != null)
                {
                    sb.AppendLine($"- {text}");
                }
                else
                {
                    sb.AppendLine(text);
                    sb.AppendLine();
                }
            }
            else if (element is Table table)
            {
                var mdTable = ConvertTableToMarkdown(table);
                if (!string.IsNullOrWhiteSpace(mdTable))
                {
                    sb.AppendLine(mdTable);
                    sb.AppendLine();
                }
            }
        }

        return Task.FromResult(sb.ToString().Trim());
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
