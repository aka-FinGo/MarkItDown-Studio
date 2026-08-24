using System.Text.RegularExpressions;
using ReverseMarkdown;

namespace MarkItDown.Core.Converters;

public class HtmlConverter
{
    private readonly Converter _converter;

    public HtmlConverter()
    {
        var config = new Config
        {
            UnknownTags = Config.UnknownTagsOption.Bypass,
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true
        };
        _converter = new Converter(config);
    }

    public string Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // Clean script and style tags
        var cleanHtml = Regex.Replace(html, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);
        cleanHtml = Regex.Replace(cleanHtml, @"<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>", "", RegexOptions.IgnoreCase);

        var markdown = _converter.Convert(cleanHtml);
        return markdown.Trim();
    }
}
