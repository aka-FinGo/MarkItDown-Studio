using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using MarkItDown.Core;
using MarkItDown.Core.Models;

namespace MarkItDown.App;

public partial class MainWindow : Window
{
    private readonly MarkItDownEngine _engine;
    private readonly AppConfig _config;
    public ObservableCollection<ConversionResult> ConvertedItems { get; } = new();
    private ConversionResult? _activeResult;
    private bool _isInitializing = true;

    public MainWindow()
    {
        InitializeComponent();
        _engine = new MarkItDownEngine();
        _config = AppConfig.Load();
        LstConvertedItems.ItemsSource = ConvertedItems;

        LoadSavedSettings();
        _isInitializing = false;
        LoadDefaultSample();
    }

    private void LoadSavedSettings()
    {
        // 1. Theme
        foreach (ComboBoxItem item in CmbTheme.Items)
        {
            if (item.Tag?.ToString() == _config.Theme)
            {
                CmbTheme.SelectedItem = item;
                ApplyTheme(_config.Theme);
                break;
            }
        }

        // 2. Provider
        foreach (ComboBoxItem item in CmbAiProvider.Items)
        {
            if (item.Tag?.ToString() == _config.SelectedProvider.ToString())
            {
                CmbAiProvider.SelectedItem = item;
                break;
            }
        }

        PopulateModelNames(_config.SelectedProvider);

        // 3. Model
        if (!string.IsNullOrEmpty(_config.SelectedModel))
        {
            CmbModelName.Text = _config.SelectedModel;
        }

        // 4. API Key
        var savedKey = _config.GetApiKey(_config.SelectedProvider);
        if (!string.IsNullOrEmpty(savedKey))
        {
            TxtApiKey.Password = savedKey;
            TxtKeyStatus.Text = "🔒 Kalit saqlangan";
            TxtKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }
        else
        {
            TxtKeyStatus.Text = "⚠️ Kalit kiritilmagan";
            TxtKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        }

        // 5. Custom URL & AI switch
        TxtCustomBaseUrl.Text = _config.CustomBaseUrl ?? "http://localhost:11434";
        ChkEnableAi.IsChecked = _config.EnableAi;
    }

    private void LoadDefaultSample()
    {
        var welcomeMd = @"# 📄 MarkItDown Studio .NET ga Xush Kelibsiz! 🚀

> 📌 **Hujjat:** `Namuna_Qollanma.md` | **Tizim:** Obsidian & Multi-AI Moslashuvchan Dvigatel

## 📑 Mundarija
- [[#1. Imkoniyatlar|1. Imkoniyatlar]]
- [[#2. Obsidian Mosligi|2. Obsidian Mosligi]]
- [[#3. Rasmlarni Boshqarish|3. Rasmlarni Boshqarish]]
- [[#4. AI Provayderlar|4. AI Provayderlar]]

---

## 1. Imkoniyatlar
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Kod va ZIP** fayllarni 100% toza matnga o'girish.
- Hech qanday ortiqcha axlat matnlarsiz, hujjatdagi asl tartib saqlanadi.

## 2. Obsidian Mosligi
- Hujjatlar Obsidian da ochilganda avtomatik Mundarija linklari (`[[#Sahifa 1]]`), chiroyli callout bloklari (`> [!NOTE]`) va jadvallar bilan ko'rinadi.

## 3. Rasmlarni Boshqarish
- Hujjat ichidagi barcha tasvirlar avtomatik alohida `{hujjat_nomi}_attachments/` papkasiga saqlanadi.
- Agar API kalit ulangan bo'lsa, har bir rasm sun'iy intellekt orqali to'liq matnga o'girilib kiritiladi!

## 4. AI Provayderlar
- **Google Gemini**, **OpenAI (GPT-4o)**, **Anthropic Claude**, **DeepSeek**, **Ollama (Lokal)** yoki **Custom Endpoint**!
- Barcha API kalitlaringiz shaxsiy kompyuteringizda xavfsiz shifrlanib saqlanadi.
";
        TxtMarkdownEditor.Text = welcomeMd;
    }

    // Title Bar Drag & Window Controls
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    // Theme Switcher
    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTheme.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Tag?.ToString() ?? "MidnightGlass";
            ApplyTheme(theme);
            if (!_isInitializing)
            {
                _config.Theme = theme;
                _config.Save();
            }
        }
    }

    private void ApplyTheme(string theme)
    {
        switch (theme)
        {
            case "ObsidianDark":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(28, 28, 28));
                SettingsBarBorder.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(168, 85, 247));
                DropIconBadge.Background = new SolidColorBrush(Color.FromRgb(88, 28, 135));
                LogoBadge.Background = new SolidColorBrush(Color.FromRgb(168, 85, 247));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                EditorInnerBorder.Background = new SolidColorBrush(Color.FromRgb(16, 16, 16));
                FooterBorder.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                break;

            case "CyberpunkNeon":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(5, 8, 17));
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(10, 15, 30));
                SettingsBarBorder.Background = new SolidColorBrush(Color.FromRgb(8, 12, 24));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                DropIconBadge.Background = new SolidColorBrush(Color.FromRgb(8, 145, 178));
                LogoBadge.Background = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                EditorInnerBorder.Background = new SolidColorBrush(Color.FromRgb(2, 6, 23));
                FooterBorder.Background = new SolidColorBrush(Color.FromRgb(5, 8, 17));
                break;

            case "FrostedCrystal":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                SettingsBarBorder.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                DropIconBadge.Background = new SolidColorBrush(Color.FromRgb(219, 234, 254));
                LogoBadge.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                EditorInnerBorder.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                FooterBorder.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                TxtAppTitle.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                TxtDocTitle.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                TxtMarkdownEditor.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                break;

            default: // MidnightGlass
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                SettingsBarBorder.Background = new SolidColorBrush(Color.FromRgb(22, 32, 50));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                DropIconBadge.Background = new SolidColorBrush(Color.FromRgb(49, 46, 129));
                LogoBadge.Background = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                EditorInnerBorder.Background = new SolidColorBrush(Color.FromRgb(11, 17, 32));
                FooterBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                TxtAppTitle.Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                TxtDocTitle.Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                TxtMarkdownEditor.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                break;
        }
    }

    // AI Provider Switcher
    private void CmbAiProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbAiProvider.SelectedItem is ComboBoxItem item && Enum.TryParse<AiProvider>(item.Tag?.ToString(), out var provider))
        {
            PopulateModelNames(provider);

            if (PnlCustomBaseUrl != null)
            {
                PnlCustomBaseUrl.Visibility = (provider == AiProvider.Ollama || provider == AiProvider.CustomOpenAICompatible)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            // Restore saved API key for this provider
            var key = _config.GetApiKey(provider);
            TxtApiKey.Password = key;
            UpdateKeyStatus(key);

            if (!_isInitializing)
            {
                _config.SelectedProvider = provider;
                _config.Save();
            }
        }
    }

    private void PopulateModelNames(AiProvider provider)
    {
        if (CmbModelName == null) return;
        CmbModelName.Items.Clear();

        if (AiProviderConfig.RecommendedModels.TryGetValue(provider, out var models))
        {
            foreach (var model in models)
            {
                CmbModelName.Items.Add(model);
            }
            CmbModelName.SelectedIndex = 0;
        }
    }

    private void TxtApiKey_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        var key = TxtApiKey.Password?.Trim() ?? string.Empty;
        var provider = GetSelectedProvider();
        _config.SetApiKey(provider, key);
        UpdateKeyStatus(key);
    }

    private void CmbModelName_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _config.SelectedModel = CmbModelName.Text?.Trim() ?? "gemini-2.5-flash";
        _config.Save();
    }

    private void TxtCustomBaseUrl_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _config.CustomBaseUrl = TxtCustomBaseUrl.Text?.Trim();
        _config.Save();
    }

    private void ChkEnableAi_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _config.EnableAi = ChkEnableAi.IsChecked == true;
        _config.Save();
    }

    private void UpdateKeyStatus(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            TxtKeyStatus.Text = "🔒 Kalit saqlandi";
            TxtKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }
        else
        {
            TxtKeyStatus.Text = "⚠️ Kalit kiritilmagan";
            TxtKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        }
    }

    private AiProvider GetSelectedProvider()
    {
        if (CmbAiProvider.SelectedItem is ComboBoxItem item && Enum.TryParse<AiProvider>(item.Tag?.ToString(), out var p))
        {
            return p;
        }
        return AiProvider.GoogleGemini;
    }

    private AiProviderConfig GetCurrentAiConfig()
    {
        var provider = GetSelectedProvider();
        return new AiProviderConfig
        {
            Provider = provider,
            ApiKey = TxtApiKey.Password?.Trim() ?? string.Empty,
            ModelName = CmbModelName.Text?.Trim() ?? "gemini-2.5-flash",
            CustomBaseUrl = TxtCustomBaseUrl.Text?.Trim()
        };
    }

    private ConversionOptions GetCurrentOptions()
    {
        return new ConversionOptions
        {
            EnableAi = ChkEnableAi.IsChecked == true,
            IncludeFrontmatter = false,
            CustomPrompt = _config.CustomPrompt,
            AutoOcrScannedPdf = true
        };
    }

    // Drag and Drop
    private void DropArea_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(129, 140, 248));
        }
    }

    private void DropArea_DragLeave(object sender, DragEventArgs e)
    {
        DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
    }

    private async void DropArea_Drop(object sender, DragEventArgs e)
    {
        DropArea_DragLeave(sender, e);
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                await ProcessFilesAsync(files);
            }
        }
    }

    private async void BtnSelectFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Konvertatsiya uchun fayllarni tanlang",
            Multiselect = true,
            Filter = "Barcha Qo'llab-quvvatlanuvchi Fayllar|*.pdf;*.docx;*.doc;*.pptx;*.ppt;*.xlsx;*.xls;*.ods;*.csv;*.tsv;*.json;*.html;*.htm;*.txt;*.py;*.cs;*.js;*.ts;*.png;*.jpg;*.jpeg;*.webp;*.mp3;*.wav;*.m4a;*.zip|Hujjatlar (*.pdf, *.docx, *.pptx, *.xlsx)|*.pdf;*.docx;*.pptx;*.xlsx|Tasvirlar va Audio (*.png, *.jpg, *.mp3, *.wav)|*.png;*.jpg;*.jpeg;*.webp;*.mp3;*.wav;*.m4a|Barcha Fayllar (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            await ProcessFilesAsync(dlg.FileNames);
        }
    }

    private async Task ProcessFilesAsync(string[] filePaths)
    {
        var options = GetCurrentOptions();
        var aiConfig = GetCurrentAiConfig();

        TxtStatus.Text = $"{filePaths.Length} ta fayl Markdown formatiga o'tkazilmoqda...";

        foreach (var path in filePaths)
        {
            try
            {
                var fileName = Path.GetFileName(path);
                TxtStatus.Text = $"Tahlil qilinmoqda: {fileName}...";

                var results = await _engine.ConvertFileAsync(path, options, aiConfig);

                foreach (var result in results)
                {
                    ConvertedItems.Insert(0, result);
                    SetActiveResult(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Faylni o'girishda xatolik ({Path.GetFileName(path)}):\n{ex.Message}", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        TxtStatus.Text = "Barcha fayllar muvaffaqiyatli o'girildi!";
    }

    private async void BtnConvertUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtWebUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Iltimos, to'g'ri HTTP/HTTPS web manzilni kiriting.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        TxtStatus.Text = $"Web sahifa yuklanmoqda: {url}...";
        try
        {
            var options = GetCurrentOptions();
            var aiConfig = GetCurrentAiConfig();

            var result = await _engine.ConvertUrlAsync(url, options, aiConfig);
            ConvertedItems.Insert(0, result);
            SetActiveResult(result);
            TxtStatus.Text = "Web sahifa muvaffaqiyatli Markdown formatiga o'girildi!";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"URL ni o'girishda xatolik:\n{ex.Message}", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "Xatolik yuz berdi.";
        }
    }

    private void SetActiveResult(ConversionResult result)
    {
        _activeResult = result;
        TxtDocTitle.Text = result.FileName;
        TxtDocStats.Text = $"Format: {result.OriginalFormat} • {result.WordCount:N0} ta so'z • {result.CharCount:N0} ta belgi • {result.DurationMs} ms • Dvigatel: {result.EngineName}";
        TxtMarkdownEditor.Text = result.Markdown;
    }

    private void LstConvertedItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstConvertedItems.SelectedItem is ConversionResult res)
        {
            SetActiveResult(res);
        }
    }

    private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
    {
        ConvertedItems.Clear();
        LoadDefaultSample();
    }

    private void BtnCopyMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtMarkdownEditor.Text))
        {
            Clipboard.SetText(TxtMarkdownEditor.Text);
            TxtStatus.Text = "Markdown matni buferga nusxalandi! (Clipboard)";
        }
    }

    private void BtnSaveMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtMarkdownEditor.Text))
        {
            MessageBox.Show("Saqlash uchun Markdown matni mavjud emas.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var defaultName = _activeResult != null
            ? $"{Path.GetFileNameWithoutExtension(_activeResult.FileName)}.md"
            : "document.md";

        var dlg = new SaveFileDialog
        {
            FileName = defaultName,
            DefaultExt = ".md",
            Filter = "Obsidian / Markdown Hujjati (*.md)|*.md|Matn Hujjati (*.txt)|*.txt|Barcha Fayllar (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, TxtMarkdownEditor.Text, System.Text.Encoding.UTF8);
            TxtStatus.Text = $"Fayl saqlandi: {dlg.FileName}";
            MessageBox.Show($"Markdown fayli muvaffaqiyatli saqlandi!\n{dlg.FileName}", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}