using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MarkItDown.Core.Models;

public class AppConfig
{
    public string Theme { get; set; } = "MidnightGlass"; // MidnightGlass, ObsidianDark, CyberpunkNeon, FrostedCrystal
    public AiProvider SelectedProvider { get; set; } = AiProvider.GoogleGemini;
    public string SelectedModel { get; set; } = "gemini-2.5-flash";
    public string? CustomBaseUrl { get; set; }
    public string? CustomPrompt { get; set; }
    public bool EnableAi { get; set; } = true;
    public bool AutoExtractImages { get; set; } = true;
    public Dictionary<string, string> SavedApiKeys { get; set; } = new();
    public Dictionary<string, List<string>> CustomModels { get; set; } = new();

    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MarkItDownStudio");

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "settings.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    // Decrypt saved API keys
                    foreach (var key in config.SavedApiKeys.Keys.ToList())
                    {
                        config.SavedApiKeys[key] = UnprotectString(config.SavedApiKeys[key]);
                    }
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Config yuklashda xatolik: {ex.Message}");
        }

        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);

            // Clone to encrypt keys for disk storage
            var clone = new AppConfig
            {
                Theme = Theme,
                SelectedProvider = SelectedProvider,
                SelectedModel = SelectedModel,
                CustomBaseUrl = CustomBaseUrl,
                CustomPrompt = CustomPrompt,
                EnableAi = EnableAi,
                AutoExtractImages = AutoExtractImages,
                CustomModels = CustomModels
            };

            foreach (var kvp in SavedApiKeys)
            {
                clone.SavedApiKeys[kvp.Key] = ProtectString(kvp.Value);
            }

            var json = JsonSerializer.Serialize(clone, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Config saqlashda xatolik: {ex.Message}");
        }
    }

    public void SetApiKey(AiProvider provider, string apiKey)
    {
        SavedApiKeys[provider.ToString()] = apiKey?.Trim() ?? string.Empty;
        Save();
    }

    public string GetApiKey(AiProvider provider)
    {
        return SavedApiKeys.TryGetValue(provider.ToString(), out var key) ? key : string.Empty;
    }

    public List<string> GetModelsForProvider(AiProvider provider)
    {
        var list = new List<string>();
        if (AiProviderConfig.RecommendedModels.TryGetValue(provider, out var defaultModels))
        {
            list.AddRange(defaultModels);
        }

        if (CustomModels.TryGetValue(provider.ToString(), out var custom) && custom != null)
        {
            foreach (var m in custom)
            {
                if (!list.Contains(m, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(m);
                }
            }
        }

        return list;
    }

    public void AddCustomModel(AiProvider provider, string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName)) return;
        var pKey = provider.ToString();
        if (!CustomModels.ContainsKey(pKey))
        {
            CustomModels[pKey] = new List<string>();
        }

        if (!CustomModels[pKey].Contains(modelName.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            CustomModels[pKey].Add(modelName.Trim());
            Save();
        }
    }

    public void RemoveCustomModel(AiProvider provider, string modelName)
    {
        var pKey = provider.ToString();
        if (CustomModels.TryGetValue(pKey, out var list))
        {
            list.RemoveAll(m => string.Equals(m, modelName, StringComparison.OrdinalIgnoreCase));
            Save();
        }
    }

    private static string ProtectString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return plainText;
        }
    }

    private static string UnprotectString(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return string.Empty;
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var decrypted = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return encryptedText;
        }
    }
}
