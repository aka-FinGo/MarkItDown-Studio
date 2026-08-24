using System.Data;
using System.Text;
using ExcelDataReader;

namespace MarkItDown.Core.Converters;

public class ExcelConverter
{
    static ExcelConverter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Task<string> ConvertAsync(byte[] excelBytes, string fileName, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(excelBytes);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var result = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = false
            }
        });

        var sb = new StringBuilder();
        var sheetCount = result.Tables.Count;
        var cleanTitle = Path.GetFileNameWithoutExtension(fileName);

        sb.AppendLine($"# 📄 {cleanTitle}");
        sb.AppendLine();
        sb.AppendLine($"> 📌 **Hujjat:** `{fileName}` | **Sahifalar (Vkladkalar):** {sheetCount} ta | **Format:** Excel");
        sb.AppendLine();

        if (sheetCount > 1)
        {
            sb.AppendLine("## 📑 Mundarija (Vkladkalar)");
            for (var i = 0; i < sheetCount; i++)
            {
                var name = result.Tables[i].TableName;
                sb.AppendLine($"- [[#Sahifa: {name}|{name}]]");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        for (var i = 0; i < sheetCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            var table = result.Tables[i];
            if (table.Rows.Count == 0) continue;

            sb.AppendLine($"## Sahifa: {table.TableName}");
            sb.AppendLine();

            var mdTable = ConvertDataTableToMarkdown(table);
            if (!string.IsNullOrWhiteSpace(mdTable))
            {
                sb.AppendLine(mdTable);
                sb.AppendLine();
            }

            if (i < sheetCount - 1)
            {
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }

        return Task.FromResult(sb.ToString().Trim());
    }

    public static string ConvertDataTableToMarkdown(DataTable table)
    {
        if (table.Rows.Count == 0) return string.Empty;

        var rows = new List<List<string>>();

        foreach (DataRow row in table.Rows)
        {
            var cells = new List<string>();
            var hasContent = false;

            for (var col = 0; col < table.Columns.Count; col++)
            {
                var val = row[col]?.ToString()?.Trim() ?? string.Empty;
                val = val.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
                cells.Add(val);

                if (!string.IsNullOrWhiteSpace(val))
                {
                    hasContent = true;
                }
            }

            if (hasContent)
            {
                rows.Add(cells);
            }
        }

        if (rows.Count == 0) return string.Empty;

        var header = rows[0];
        var separator = header.Select(_ => "---").ToList();
        var dataRows = rows.Skip(1).ToList();

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
