namespace MarkItDown.Core.Models;

public class ConversionOptions
{
    public bool EnableAi { get; set; } = false;
    public bool IncludeFrontmatter { get; set; } = false;
    public bool IncludeSummary { get; set; } = false;
    public string? CustomPrompt { get; set; }
    public string TableStyle { get; set; } = "standard";
    public bool AutoOcrScannedPdf { get; set; } = true;
}

public class ConversionResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = string.Empty;
    public string OriginalFormat { get; set; } = string.Empty;
    public long OriginalSizeBytes { get; set; }
    public string Markdown { get; set; } = string.Empty;
    public long MarkdownSizeBytes => System.Text.Encoding.UTF8.GetByteCount(Markdown ?? string.Empty);
    public int WordCount { get; set; }
    public int CharCount { get; set; }
    public int LineCount { get; set; }
    public int EstimatedTokens { get; set; }
    public long DurationMs { get; set; }
    public bool UsedAi { get; set; }
    public int TokensConsumed { get; set; }
    public string EngineName { get; set; } = "MarkItDown .NET Engine";
    public string? ErrorMessage { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? SourceUrl { get; set; }
    public Dictionary<string, object>? Frontmatter { get; set; }
}

public enum AiProvider
{
    GoogleGemini,
    OpenAI,
    AnthropicClaude,
    DeepSeek,
    Ollama,
    CustomOpenAICompatible
}

public class AiProviderConfig
{
    public AiProvider Provider { get; set; } = AiProvider.GoogleGemini;
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-2.5-flash";
    public string? CustomBaseUrl { get; set; }
    public double Temperature { get; set; } = 0.1;

    public static readonly Dictionary<AiProvider, string[]> RecommendedModels = new()
    {
        [AiProvider.GoogleGemini] = ["gemini-2.5-flash", "gemini-3.7-flash", "gemini-2.5-pro", "gemini-flash-lite"],
        [AiProvider.OpenAI] = ["gpt-4o", "gpt-4o-mini", "o1", "o3-mini"],
        [AiProvider.AnthropicClaude] = ["claude-3-7-sonnet-20250219", "claude-3-5-haiku-20241022", "claude-3-5-sonnet-20241022"],
        [AiProvider.DeepSeek] = ["deepseek-chat", "deepseek-reasoner"],
        [AiProvider.Ollama] = ["llama3.2-vision", "llama3.3", "mistral", "qwen2.5-coder", "deepseek-r1:8b"],
        [AiProvider.CustomOpenAICompatible] = ["default"]
    };
}
