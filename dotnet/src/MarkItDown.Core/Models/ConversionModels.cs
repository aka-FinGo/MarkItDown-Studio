namespace MarkItDown.Core.Models;

public enum AiProvider
{
    GoogleGemini,
    GroqAI,
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

    public static readonly Dictionary<AiProvider, List<string>> RecommendedModels = new()
    {
        [AiProvider.GoogleGemini] = new()
        {
            "gemini-2.5-flash",
            "gemini-2.5-pro",
            "gemini-3.7-flash",
            "gemini-3-flash",
            "gemini-3-pro",
            "gemini-3-deep-think",
            "gemini-2.5-flash-lite",
            "gemini-2.5-flash-image",
            "gemini-2.0-flash",
            "gemini-1.5-pro",
            "gemini-1.5-flash",
            "gemini-live-2.5-flash-preview-native-audio-09-2025"
        },
        [AiProvider.GroqAI] = new()
        {
            "llama-3.3-70b-versatile",
            "llama-3.1-8b-instant",
            "deepseek-r1-distill-llama-70b",
            "qwen-qwq-32b",
            "mistral-saba-24b",
            "gemma2-9b-it",
            "whisper-large-v3-turbo",
            "whisper-large-v3",
            "meta-llama/llama-4-maverick-17b-128e-instruct",
            "meta-llama/llama-4-scout-17b-16e-instruct"
        },
        [AiProvider.OpenAI] = new()
        {
            "gpt-4o",
            "gpt-4o-mini",
            "o3-mini",
            "o1",
            "gpt-4-turbo"
        },
        [AiProvider.AnthropicClaude] = new()
        {
            "claude-3-7-sonnet-20250219",
            "claude-3-5-sonnet-20241022",
            "claude-3-5-haiku-20241022",
            "claude-3-opus-20240229"
        },
        [AiProvider.DeepSeek] = new()
        {
            "deepseek-chat",
            "deepseek-reasoner"
        },
        [AiProvider.Ollama] = new()
        {
            "llama3.2-vision",
            "llava:latest",
            "qwen2.5-vl:latest",
            "mistral:latest",
            "deepseek-r1:latest"
        },
        [AiProvider.CustomOpenAICompatible] = new()
        {
            "default-model"
        }
    };

    public static readonly Dictionary<AiProvider, string> ProviderGuide = new()
    {
        [AiProvider.GoogleGemini] = "💡 Google AI Studio (aistudio.google.com/app/apikey) ga kiring va 1 daqiqada 100% bepul API kalit oling.",
        [AiProvider.GroqAI] = "💡 Groq Console (console.groq.com/keys) ga kiring va chaqmoqdek tezkor (500+ tok/s) bepul API kalit oling.",
        [AiProvider.OpenAI] = "💡 OpenAI Platform (platform.openai.com/api-keys) ga kiring va yangi Secret Key yarating.",
        [AiProvider.AnthropicClaude] = "💡 Anthropic Console (console.anthropic.com/settings/keys) orqali Claude API kalit oling.",
        [AiProvider.DeepSeek] = "💡 DeepSeek Platform (platform.deepseek.com/api_keys) dan arzon va tezkor API kalit oling.",
        [AiProvider.Ollama] = "💡 Kompyuteringizda 'ollama run llama3.2-vision' buyrug'ini bering (API kalit shart emas, 100% lokal).",
        [AiProvider.CustomOpenAICompatible] = "💡 O'zingizning OpenAI-mos serveringiz manzilini (Base URL) va API kalitingizni kiriting."
    };

    public static readonly Dictionary<string, string> FallbackModels = new()
    {
        ["gemini-3-pro"] = "gemini-2.5-pro",
        ["gemini-2.5-pro"] = "gemini-2.5-flash",
        ["gemini-3-flash"] = "gemini-2.5-flash",
        ["gemini-3.7-flash"] = "gemini-2.5-flash",
        ["gemini-2.5-flash"] = "gemini-2.5-flash-lite",
        ["llama-3.3-70b-versatile"] = "llama-3.1-8b-instant",
        ["deepseek-r1-distill-llama-70b"] = "llama-3.3-70b-versatile",
        ["gpt-4o"] = "gpt-4o-mini",
        ["claude-3-7-sonnet-20250219"] = "claude-3-5-sonnet-20241022",
        ["claude-3-5-sonnet-20241022"] = "claude-3-5-haiku-20241022",
        ["deepseek-reasoner"] = "deepseek-chat"
    };
}

public class ConversionOptions
{
    public bool EnableAi { get; set; } = true;
    public bool IncludeFrontmatter { get; set; } = false;
    public string? CustomPrompt { get; set; }
    public bool AutoOcrScannedPdf { get; set; } = true;
}

public class ConversionResult
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFormat { get; set; } = string.Empty;
    public long OriginalSizeBytes { get; set; }
    public string Markdown { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int CharCount { get; set; }
    public int LineCount { get; set; }
    public int EstimatedTokens { get; set; }
    public long DurationMs { get; set; }
    public bool UsedAi { get; set; }
    public int TokensConsumed { get; set; }
    public string? SourceUrl { get; set; }
    public string EngineName { get; set; } = "Local";
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
