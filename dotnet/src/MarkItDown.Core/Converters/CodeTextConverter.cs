using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CsvHelper;
using CsvHelper.Configuration;

namespace MarkItDown.Core.Converters;

public class CodeTextConverter
{
    private static readonly Dictionary<string, string> ExtensionToLanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cs"] = "csharp",
        ["ts"] = "typescript",
        ["tsx"] = "tsx",
        ["js"] = "javascript",
        ["jsx"] = "jsx",
        ["py"] = "python",
        ["java"] = "java",
        ["cpp"] = "cpp",
        ["c"] = "c",
        ["h"] = "c",
        ["hpp"] = "cpp",
        ["go"] = "go",
        ["rs"] = "rust",
        ["sql"] = "sql",
        ["sh"] = "bash",
        ["bash"] = "bash",
        ["ps1"] = "powershell",
        ["bat"] = "batch",
        ["cmd"] = "batch",
        ["css"] = "css",
        ["scss"] = "scss",
        ["html"] = "html",
        ["xml"] = "xml",
        ["xaml"] = "xml",
        ["yaml"] = "yaml",
        ["yml"] = "yaml",
        ["json"] = "json",
        ["md"] = "markdown",
        ["txt"] = "text",
        ["log"] = "log"
    };

    public string ConvertCode(string code, string extension)
    {
        var lang = ExtensionToLanguageMap.TryGetValue(extension, out var l) ? l : string.Empty;
        return $"```{lang}\n{code}\n```";
    }

    public string ConvertCsv(string csvContent, string delimiter = ",")
    {
        using var reader = new StringReader(csvContent);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            BadDataFound = null,
            MissingFieldFound = null
        };
        using var csv = new CsvReader(reader, config);

        var rows = new List<List<string>>();
        while (csv.Read())
        {
            var row = new List<string>();
            for (var i = 0; csv.TryGetField<string>(i, out var field); i++)
            {
                var clean = (field ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
                row.Add(clean);
            }
            if (row.Any(c => !string.IsNullOrWhiteSpace(c)))
            {
                rows.Add(row);
            }
        }

        if (rows.Count == 0) return string.Empty;

        var maxCols = rows.Max(r => r.Count);
        foreach (var row in rows)
        {
            while (row.Count < maxCols) row.Add(string.Empty);
        }

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

    public string ConvertJson(string jsonContent)
    {
        try
        {
            var node = JsonNode.Parse(jsonContent);
            if (node is JsonArray array && array.Count > 0 && array[0] is JsonObject)
            {
                // Format array of objects as GFM Markdown table
                var keys = new HashSet<string>();
                foreach (var item in array)
                {
                    if (item is JsonObject obj)
                    {
                        foreach (var kvp in obj) keys.Add(kvp.Key);
                    }
                }

                var keyList = keys.ToList();
                var sb = new StringBuilder();
                sb.AppendLine($"| {string.Join(" | ", keyList)} |");
                sb.AppendLine($"| {string.Join(" | ", keyList.Select(_ => "---"))} |");

                foreach (var item in array)
                {
                    if (item is JsonObject obj)
                    {
                        var row = keyList.Select(k => (obj[k]?.ToString() ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " "));
                        sb.AppendLine($"| {string.Join(" | ", row)} |");
                    }
                }

                return sb.ToString().Trim();
            }

            using var doc = JsonDocument.Parse(jsonContent);
            return "```json\n" + JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }) + "\n```";
        }
        catch
        {
            return "```json\n" + jsonContent + "\n```";
        }
    }
}
