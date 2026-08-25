using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using MarkItDown.Core;
using MarkItDown.Core.Ai;
using MarkItDown.Core.Models;
using MarkItDown.Core.Ocr;

namespace MarkItDown.App;

public class PendingFileItem
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public required string FileSizeText { get; set; }
    public long FileSizeBytes { get; set; }
}

public partial class MainWindow : Window
{
    private readonly MarkItDownEngine _engine;
    private readonly UniversalAiClient _aiClient;
    private readonly AppConfig _config;
    public ObservableCollection<ConversionResult> ConvertedItems { get; } = new();
    public ObservableCollection<PendingFileItem> SelectedFilesQueue { get; } = new();
    private ConversionResult? _activeResult;
    private bool _isInitializing = true;
    private string _currentLang = "uz";

    public MainWindow()
    {
        InitializeComponent();
        _engine = new MarkItDownEngine();
        _aiClient = new UniversalAiClient();
        _config = AppConfig.Load();
        LstConvertedItems.ItemsSource = ConvertedItems;
        LstSelectedFilesQueue.ItemsSource = SelectedFilesQueue;

        LoadSavedSettings();
        _isInitializing = false;
        LoadDefaultSample();
    }

    private void LoadSavedSettings()
    {
        // 1. Language
        if (CmbLanguage != null)
        {
            var found = false;
            foreach (ComboBoxItem item in CmbLanguage.Items)
            {
                if (item.Tag?.ToString() == "uz")
                {
                    CmbLanguage.SelectedItem = item;
                    ApplyLanguage("uz");
                    found = true;
                    break;
                }
            }
            if (!found && CmbLanguage.Items.Count > 0)
            {
                CmbLanguage.SelectedIndex = 0;
                ApplyLanguage("uz");
            }
        }

        // 2. Theme
        if (CmbTheme != null)
        {
            var found = false;
            foreach (ComboBoxItem item in CmbTheme.Items)
            {
                if (item.Tag?.ToString() == _config.Theme)
                {
                    CmbTheme.SelectedItem = item;
                    ApplyTheme(_config.Theme);
                    found = true;
                    break;
                }
            }
            if (!found && CmbTheme.Items.Count > 0)
            {
                CmbTheme.SelectedIndex = 0;
                ApplyTheme("MidnightGlass");
            }
        }

        // 3. Provider
        if (CmbAiProvider != null)
        {
            var found = false;
            foreach (ComboBoxItem item in CmbAiProvider.Items)
            {
                if (item.Tag?.ToString() == _config.SelectedProvider.ToString())
                {
                    CmbAiProvider.SelectedItem = item;
                    found = true;
                    break;
                }
            }
            if (!found && CmbAiProvider.Items.Count > 0)
            {
                CmbAiProvider.SelectedIndex = 0;
            }
        }

        PopulateModelNames(_config.SelectedProvider);

        // 4. Model
        if (CmbModelName != null && !string.IsNullOrEmpty(_config.SelectedModel))
        {
            CmbModelName.Text = _config.SelectedModel;
        }

        // 5. API Key
        var savedKey = _config.GetApiKey(_config.SelectedProvider);
        if (TxtApiKey != null)
        {
            TxtApiKey.Password = savedKey;
        }
        UpdateKeyStatusAndGuide(_config.SelectedProvider, savedKey);

        // 6. Custom URL
        if (TxtCustomBaseUrl != null)
        {
            TxtCustomBaseUrl.Text = _config.CustomBaseUrl ?? "http://localhost:11434";
        }
    }

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || CmbLanguage == null) return;
        if (CmbLanguage.SelectedItem is ComboBoxItem item)
        {
            var lang = item.Tag?.ToString() ?? "uz";
            ApplyLanguage(lang);
        }
    }

    private void ApplyLanguage(string lang)
    {
        _currentLang = lang;
        switch (lang)
        {
            case "en":
                if (TxtFormatsBtnLabel != null) TxtFormatsBtnLabel.Text = "Formats";
                if (TxtApiKeyBtnLabel != null) TxtApiKeyBtnLabel.Text = "API Key:";
                if (BtnTabUpload != null) BtnTabUpload.Content = "📁 Drop files here";
                if (TxtDropTitle1 != null) TxtDropTitle1.Text = "Drop your files here or ";
                if (TxtDropTitle2 != null) TxtDropTitle2.Text = "Select Files...";
                if (TxtDropSubtitle != null) TxtDropSubtitle.Text = "PDF, Word, Excel, PPTX, Images (OCR), Audio, ZIP, Code";
                if (BtnStartConvertFiles != null) BtnStartConvertFiles.Content = "✨ Convert Files to Markdown (.md)";
                if (BtnConvertUrl != null) BtnConvertUrl.Content = "Convert URL";
                if (BtnClearAll != null) BtnClearAll.Content = "🗑️ Clear All";
                if (BtnAiProofread != null) BtnAiProofread.Content = "✨ AI Proofread";
                if (BtnCopyMarkdown != null) BtnCopyMarkdown.Content = "📋 Copy";
                if (BtnSaveMarkdown != null) BtnSaveMarkdown.Content = "💾 Save .md";
                if (TxtModalHeader != null) TxtModalHeader.Text = "AI Analysis & Error Correction Result";
                if (BtnModalCancel != null) BtnModalCancel.Content = "Cancel";
                if (BtnModalApply != null) BtnModalApply.Content = "✅ Apply to Document";
                break;

            case "ru":
                if (TxtFormatsBtnLabel != null) TxtFormatsBtnLabel.Text = "Форматы";
                if (TxtApiKeyBtnLabel != null) TxtApiKeyBtnLabel.Text = "API Ключ:";
                if (BtnTabUpload != null) BtnTabUpload.Content = "📁 Перетащите файлы сюда";
                if (TxtDropTitle1 != null) TxtDropTitle1.Text = "Перетащите файлы сюда или ";
                if (TxtDropTitle2 != null) TxtDropTitle2.Text = "Выбрать файлы...";
                if (TxtDropSubtitle != null) TxtDropSubtitle.Text = "PDF, Word, Excel, PPTX, Изображения (OCR), Аудио, ZIP, Код";
                if (BtnStartConvertFiles != null) BtnStartConvertFiles.Content = "✨ Преобразовать файлы в Markdown (.md)";
                if (BtnConvertUrl != null) BtnConvertUrl.Content = "Конвертировать URL";
                if (BtnClearAll != null) BtnClearAll.Content = "🗑️ Очистить всё";
                if (BtnAiProofread != null) BtnAiProofread.Content = "✨ Проверить с ИИ";
                if (BtnCopyMarkdown != null) BtnCopyMarkdown.Content = "📋 Копировать";
                if (BtnSaveMarkdown != null) BtnSaveMarkdown.Content = "💾 Сохранить .md";
                if (TxtModalHeader != null) TxtModalHeader.Text = "Результат анализа и исправления ИИ";
                if (BtnModalCancel != null) BtnModalCancel.Content = "Отмена";
                if (BtnModalApply != null) BtnModalApply.Content = "✅ Применить к документу";
                break;

            default: // uz
                if (TxtFormatsBtnLabel != null) TxtFormatsBtnLabel.Text = "Formatlar";
                if (TxtApiKeyBtnLabel != null) TxtApiKeyBtnLabel.Text = "API Kalit:";
                if (BtnTabUpload != null) BtnTabUpload.Content = "📁 Faylni bu yerga tashlang";
                if (TxtDropTitle1 != null) TxtDropTitle1.Text = "Faylni bu yerga tashlang yoki ";
                if (TxtDropTitle2 != null) TxtDropTitle2.Text = "Fayl Tanlash...";
                if (TxtDropSubtitle != null) TxtDropSubtitle.Text = "PDF, Word, Excel, PPTX, Rasm (OCR), Audio, ZIP, Kod";
                if (BtnStartConvertFiles != null) BtnStartConvertFiles.Content = "✨ Matnni Markdown (.md) ga O'tkazish";
                if (BtnConvertUrl != null) BtnConvertUrl.Content = "URL O'girish";
                if (BtnClearAll != null) BtnClearAll.Content = "🗑️ Tozalash";
                if (BtnAiProofread != null) BtnAiProofread.Content = "✨ AI Bilan Tekshirish";
                if (BtnCopyMarkdown != null) BtnCopyMarkdown.Content = "📋 Nusxa olish";
                if (BtnSaveMarkdown != null) BtnSaveMarkdown.Content = "💾 .md Saqlash";
                if (TxtModalHeader != null) TxtModalHeader.Text = "AI Tahlili va Xatoliklarni Tuzatish Natijasi";
                if (BtnModalCancel != null) BtnModalCancel.Content = "Bekor Qilish";
                if (BtnModalApply != null) BtnModalApply.Content = "✅ Tasdiqlash va Hujjatga Qo'llash";
                break;
        }

        UpdateKeyStatusAndGuide(GetSelectedProvider(), TxtApiKey?.Password?.Trim() ?? string.Empty);
    }

    private void LoadDefaultSample()
    {
        var welcomeMd = _currentLang switch
        {
            "en" => @"# 📄 Welcome to MarkItDown Studio! 🚀

> 📌 **Document:** `Sample_Guide.md` | **System:** Multi-AI Smart Fallback Engine & Windows OCR

## 1. Capabilities
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Code and Images** converted to 100% clean Markdown.
- **Queue Workflow:** Select files first, check the list, and click **✨ Convert Files to Markdown (.md)**!

## 2. Universal AI Providers & Models
- **Google Gemini:** `gemini-2.5-flash`, `gemini-2.5-pro`, `gemini-3.7-flash`
- **Groq AI (Ultra-Fast 500+ tok/s):** `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`, `deepseek-r1-distill-llama-70b`
- **OpenAI, Claude 3.7, DeepSeek R1/V3, Ollama**.
",
            "ru" => @"# 📄 Добро пожаловать в MarkItDown Studio! 🚀

> 📌 **Документ:** `Руководство.md` | **Система:** Мульти-ИИ движок с авто-переключением и Windows OCR

## 1. Возможности
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Код и Изображения** в 100% чистый Markdown.
- **Очередь файлов:** Выберите файлы и нажмите **✨ Преобразовать файлы в Markdown (.md)**!

## 2. Универсальные ИИ Провайдеры
- **Google Gemini, Groq AI (500+ токенов/сек), OpenAI, Claude, DeepSeek, Ollama**.
",
            _ => @"# 📄 MarkItDown Studio ga Xush Kelibsiz! 🚀

> 📌 **Hujjat:** `Namuna_Qollanma.md` | **Tizim:** Multi-AI Smart Fallback Dvigatel & Windows OCR

## 1. Imkoniyatlar
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Kod va Rasmlar** 100% toza Markdown formatiga o'tkaziladi.
- **Fayllar Navbati:** Fayllarni tanlang, ro'yxatni ko'ring va **✨ Matnni Markdown (.md) ga O'tkazish** tugmasini bosing!

## 2. Universal AI Provayderlar
- **Google Gemini, Groq AI (500+ tok/s), OpenAI, Claude, DeepSeek, Ollama**.
"
        };

        if (TxtMarkdownEditor != null)
        {
            TxtMarkdownEditor.Text = welcomeMd;
        }
    }

    // Tabs
    private void BtnTabUpload_Click(object sender, RoutedEventArgs e)
    {
        PnlUploadMode.Visibility = Visibility.Visible;
        PnlUrlMode.Visibility = Visibility.Collapsed;
        BtnTabUpload.Style = (Style)FindResource("AccentButton");
        BtnTabUrl.Style = (Style)FindResource("GlassButton");
    }

    private void BtnTabUrl_Click(object sender, RoutedEventArgs e)
    {
        PnlUploadMode.Visibility = Visibility.Collapsed;
        PnlUrlMode.Visibility = Visibility.Visible;
        BtnTabUpload.Style = (Style)FindResource("GlassButton");
        BtnTabUrl.Style = (Style)FindResource("AccentButton");
    }

    // Modals
    private void BtnOpenApiKey_Click(object sender, RoutedEventArgs e) => PnlApiKeyModal.Visibility = Visibility.Visible;
    private void BtnCloseApiKeyModal_Click(object sender, RoutedEventArgs e)
    {
        PnlApiKeyModal.Visibility = Visibility.Collapsed;
        _config.SelectedProvider = GetSelectedProvider();
        _config.SelectedModel = CmbModelName?.Text?.Trim() ?? "gemini-2.5-flash";
        _config.SetApiKey(_config.SelectedProvider, TxtApiKey?.Password?.Trim() ?? string.Empty);
        _config.CustomBaseUrl = TxtCustomBaseUrl?.Text?.Trim();
        _config.Save();
        UpdateKeyStatusAndGuide(_config.SelectedProvider, TxtApiKey?.Password?.Trim() ?? string.Empty);
    }

    private void BtnOpenFormats_Click(object sender, RoutedEventArgs e) => PnlFormatsModal.Visibility = Visibility.Visible;
    private void BtnCloseFormatsModal_Click(object sender, RoutedEventArgs e) => PnlFormatsModal.Visibility = Visibility.Collapsed;

    // Window KeyDown (Ctrl+V paste image/text)
    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (TxtMarkdownEditor.IsFocused || TxtWebUrl.IsFocused || TxtApiKey.IsFocused) return;

            if (Clipboard.ContainsImage())
            {
                e.Handled = true;
                var bitmapSource = Clipboard.GetImage();
                if (bitmapSource != null)
                {
                    byte[] imageBytes;
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
                    using (var ms = new MemoryStream())
                    {
                        encoder.Save(ms);
                        imageBytes = ms.ToArray();
                    }

                    var fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    var results = await _engine.ConvertBytesAsync(imageBytes, fileName, null, GetCurrentOptions(), GetCurrentAiConfig());

                    foreach (var res in results)
                    {
                        ConvertedItems.Insert(0, res);
                        SetActiveResult(res);
                    }
                }
            }
        }
    }

    // Title Bar Drag & Window Controls
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMaximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    // Theme Switcher
    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || CmbTheme == null) return;
        if (CmbTheme.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Tag?.ToString() ?? "MidnightGlass";
            ApplyTheme(theme);
            _config.Theme = theme;
            _config.Save();
        }
    }

    private void ApplyTheme(string theme)
    {
        if (MainBorder == null || TitleBarBorder == null || DropArea == null || EditorContainerBorder == null) return;

        switch (theme)
        {
            case "ObsidianDark":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(28, 28, 28));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(28, 28, 28));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                break;
            case "CyberpunkNeon":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(5, 8, 17));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(10, 15, 30));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                break;
            case "FrostedCrystal":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                break;
            default: // MidnightGlass
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(5, 8, 20));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(9, 14, 26));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(10, 16, 29));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(10, 16, 29));
                break;
        }
    }

    // AI Provider Switcher
    private void CmbAiProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || CmbAiProvider == null) return;
        if (CmbAiProvider.SelectedItem is ComboBoxItem item && Enum.TryParse<AiProvider>(item.Tag?.ToString(), out var provider))
        {
            PopulateModelNames(provider);

            if (PnlCustomBaseUrl != null)
            {
                PnlCustomBaseUrl.Visibility = (provider == AiProvider.Ollama || provider == AiProvider.CustomOpenAICompatible)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            var key = _config.GetApiKey(provider);
            if (TxtApiKey != null) TxtApiKey.Password = key;
            UpdateKeyStatusAndGuide(provider, key);
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
            if (CmbModelName.Items.Count > 0) CmbModelName.SelectedIndex = 0;
        }
    }

    private void TxtApiKey_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || TxtApiKey == null) return;
        var key = TxtApiKey.Password?.Trim() ?? string.Empty;
        var provider = GetSelectedProvider();
        UpdateKeyStatusAndGuide(provider, key);
    }

    private void CmbModelName_LostFocus(object sender, RoutedEventArgs e) { }
    private void TxtCustomBaseUrl_LostFocus(object sender, RoutedEventArgs e) { }

    private void UpdateKeyStatusAndGuide(AiProvider provider, string key)
    {
        if (TxtApiKeyBtnLabel != null)
        {
            TxtApiKeyBtnLabel.Text = !string.IsNullOrEmpty(key) ? "API Kalit: Saqlangan ✓" : "API Kalit: Yo'q";
        }
        if (TxtKeyGuide != null && AiProviderConfig.ProviderGuide.TryGetValue(provider, out var guide))
        {
            TxtKeyGuide.Text = guide;
        }
    }

    private AiProvider GetSelectedProvider()
    {
        if (CmbAiProvider?.SelectedItem is ComboBoxItem item && Enum.TryParse<AiProvider>(item.Tag?.ToString(), out var p))
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
            ApiKey = TxtApiKey?.Password?.Trim() ?? string.Empty,
            ModelName = CmbModelName?.Text?.Trim() ?? "gemini-2.5-flash",
            CustomBaseUrl = TxtCustomBaseUrl?.Text?.Trim()
        };
    }

    private ConversionOptions GetCurrentOptions()
    {
        return new ConversionOptions
        {
            EnableAi = true,
            IncludeFrontmatter = false,
            CustomPrompt = _config.CustomPrompt,
            AutoOcrScannedPdf = true
        };
    }

    // Drag & Drop Queue (Does NOT convert automatically until clicked!)
    private void DropArea_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && DropArea != null)
        {
            DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(129, 140, 248));
        }
    }

    private void DropArea_DragLeave(object sender, DragEventArgs e)
    {
        if (DropArea != null)
        {
            DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(49, 46, 129));
        }
    }

    private void DropArea_Drop(object sender, DragEventArgs e)
    {
        DropArea_DragLeave(sender, e);
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                AddFilesToQueue(files);
            }
        }
    }

    private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
    {
        var filterText = _currentLang switch
        {
            "en" => "All Supported Files|*.pdf;*.docx;*.doc;*.pptx;*.ppt;*.xlsx;*.xls;*.ods;*.csv;*.tsv;*.json;*.html;*.htm;*.txt;*.py;*.cs;*.js;*.ts;*.png;*.jpg;*.jpeg;*.webp;*.mp3;*.wav;*.m4a;*.zip|All Files (*.*)|*.*",
            "ru" => "Все поддерживаемые файлы|*.pdf;*.docx;*.doc;*.pptx;*.ppt;*.xlsx;*.xls;*.ods;*.csv;*.tsv;*.json;*.html;*.htm;*.txt;*.py;*.cs;*.js;*.ts;*.png;*.jpg;*.jpeg;*.webp;*.mp3;*.wav;*.m4a;*.zip|Все файлы (*.*)|*.*",
            _ => "Barcha Qo'llab-quvvatlanuvchi Fayllar|*.pdf;*.docx;*.doc;*.pptx;*.ppt;*.xlsx;*.xls;*.ods;*.csv;*.tsv;*.json;*.html;*.htm;*.txt;*.py;*.cs;*.js;*.ts;*.png;*.jpg;*.jpeg;*.webp;*.mp3;*.wav;*.m4a;*.zip|Barcha Fayllar (*.*)|*.*"
        };

        var dlg = new OpenFileDialog
        {
            Title = _currentLang switch { "en" => "Select files to convert", "ru" => "Выберите файлы для конвертации", _ => "Konvertatsiya uchun fayllarni tanlang" },
            Multiselect = true,
            Filter = filterText
        };

        if (dlg.ShowDialog() == true)
        {
            AddFilesToQueue(dlg.FileNames);
        }
    }

    private void AddFilesToQueue(string[] filePaths)
    {
        foreach (var path in filePaths)
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var sizeStr = fileInfo.Length < 1024 ? $"{fileInfo.Length} B" :
                              fileInfo.Length < 1024 * 1024 ? $"{fileInfo.Length / 1024.0:F1} KB" :
                              $"{fileInfo.Length / (1024.0 * 1024.0):F2} MB";

                SelectedFilesQueue.Add(new PendingFileItem
                {
                    FilePath = path,
                    FileName = Path.GetFileName(path),
                    FileSizeText = sizeStr,
                    FileSizeBytes = fileInfo.Length
                });
            }
        }

        UpdateQueueDisplay();
    }

    private void UpdateQueueDisplay()
    {
        if (PnlSelectedFilesQueue != null && TxtQueueSummary != null && TxtQueueReadyStatus != null)
        {
            if (SelectedFilesQueue.Count > 0)
            {
                PnlSelectedFilesQueue.Visibility = Visibility.Visible;
                var totalBytes = SelectedFilesQueue.Sum(f => f.FileSizeBytes);
                var totalSizeStr = totalBytes < 1024 ? $"{totalBytes} B" :
                                   totalBytes < 1024 * 1024 ? $"{totalBytes / 1024.0:F1} KB" :
                                   $"{totalBytes / (1024.0 * 1024.0):F2} MB";

                TxtQueueSummary.Text = $"Tanlangan ({SelectedFilesQueue.Count}) Jami: {totalSizeStr}";
                TxtQueueReadyStatus.Text = $"{SelectedFilesQueue.Count} ta fayl .md formatiga o'tkazishga tayyor";
            }
            else
            {
                PnlSelectedFilesQueue.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void BtnRemoveFromQueue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PendingFileItem item)
        {
            SelectedFilesQueue.Remove(item);
            UpdateQueueDisplay();
        }
    }

    private void BtnClearSelectedQueue_Click(object sender, RoutedEventArgs e)
    {
        SelectedFilesQueue.Clear();
        UpdateQueueDisplay();
    }

    // ✨ Convert Button: ONLY converts when user clicks this button!
    private async void BtnStartConvertFiles_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedFilesQueue.Count == 0) return;

        var filesToProcess = SelectedFilesQueue.Select(f => f.FilePath).ToArray();
        SelectedFilesQueue.Clear();
        UpdateQueueDisplay();

        await ProcessFilesAsync(filesToProcess);
    }

    private async Task ProcessFilesAsync(string[] filePaths)
    {
        var options = GetCurrentOptions();
        var aiConfig = GetCurrentAiConfig();

        foreach (var path in filePaths)
        {
            try
            {
                var fileName = Path.GetFileName(path);
                var results = await _engine.ConvertFileAsync(path, options, aiConfig);

                foreach (var result in results)
                {
                    ConvertedItems.Insert(0, result);
                    SetActiveResult(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Xatolik ({Path.GetFileName(path)}):\n{ex.Message}", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void BtnConvertUrl_Click(object sender, RoutedEventArgs e)
    {
        if (TxtWebUrl == null) return;
        var url = TxtWebUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("HTTP/HTTPS URL kiriting.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = GetCurrentOptions();
            var aiConfig = GetCurrentAiConfig();

            var result = await _engine.ConvertUrlAsync(url, options, aiConfig);
            ConvertedItems.Insert(0, result);
            SetActiveResult(result);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"URL error:\n{ex.Message}", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetActiveResult(ConversionResult result)
    {
        _activeResult = result;
        if (TxtDocStats != null) TxtDocStats.Text = $"{result.FileName} • {result.WordCount:N0} ta so'z • {result.CharCount:N0} ta belgi";
        if (TxtDocEngine != null) TxtDocEngine.Text = $"⏱ {result.DurationMs} ms • {result.EngineName}";
        if (TxtMarkdownEditor != null) TxtMarkdownEditor.Text = result.Markdown;
    }

    private void LstConvertedItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstConvertedItems.SelectedItem is ConversionResult res)
        {
            SetActiveResult(res);
        }
    }

    private void BtnDeleteSingleItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ConversionResult item)
        {
            ConvertedItems.Remove(item);
            if (_activeResult == item)
            {
                if (ConvertedItems.Count > 0) SetActiveResult(ConvertedItems[0]);
                else LoadDefaultSample();
            }
        }
    }

    private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
    {
        if (ConvertedItems.Count == 0) return;
        var res = MessageBox.Show("Haqiqatan ham barcha o'girilgan fayllar tarixini tozalamoqchimisiz?", "Tarixni tozalash", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            ConvertedItems.Clear();
            LoadDefaultSample();
        }
    }

    // AI Proofreader
    private async void BtnAiProofread_Click(object sender, RoutedEventArgs e)
    {
        var currentText = TxtMarkdownEditor?.Text?.Trim();
        if (string.IsNullOrEmpty(currentText))
        {
            MessageBox.Show("Markdown matni mavjud emas.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var aiConfig = GetCurrentAiConfig();
        if (aiConfig.Provider != AiProvider.Ollama && string.IsNullOrWhiteSpace(aiConfig.ApiKey))
        {
            PnlApiKeyModal.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var prompt = @"Ushbu Markdown hujjatidagi barcha orfografik, grammatik xatoliklarni, buzilgan jadvallarni va Lotin/Krill chalkashliklarini (o'/ў, g'/ғ, sh/ш, ch/ч) to'liq to'g'rilab, toza va chiroyli Markdown qilib qaytar. Ortiqcha izohlarsiz, faqat to'g'rilangan yakuniy Markdownni ber.";
            var rawBytes = System.Text.Encoding.UTF8.GetBytes(currentText);

            var (correctedMd, _) = await _aiClient.ConvertWithAiAsync(rawBytes, "text/plain", "document_review.md", aiConfig, prompt);

            if (TxtAiProofreadResult != null) TxtAiProofreadResult.Text = correctedMd;
            if (PnlAiReviewModal != null) PnlAiReviewModal.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"AI error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCloseReviewModal_Click(object sender, RoutedEventArgs e) => PnlAiReviewModal.Visibility = Visibility.Collapsed;

    private void BtnApplyAiProofread_Click(object sender, RoutedEventArgs e)
    {
        var corrected = TxtAiProofreadResult?.Text;
        if (!string.IsNullOrEmpty(corrected))
        {
            if (TxtMarkdownEditor != null) TxtMarkdownEditor.Text = corrected;
            if (_activeResult != null)
            {
                _activeResult.Markdown = corrected;
                _activeResult.WordCount = MarkItDownEngine.CountWords(corrected);
                _activeResult.CharCount = corrected.Length;
                _activeResult.EstimatedTokens = MarkItDownEngine.EstimateTokens(corrected);
                if (TxtDocStats != null) TxtDocStats.Text = $"{_activeResult.FileName} • {_activeResult.WordCount:N0} ta so'z • {_activeResult.CharCount:N0} ta belgi (AI Verified)";
            }
            if (PnlAiReviewModal != null) PnlAiReviewModal.Visibility = Visibility.Collapsed;
            MessageBox.Show("Markdown hujjati AI tuzatishlari bilan yangilandi!", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnCopyMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (TxtMarkdownEditor != null && !string.IsNullOrEmpty(TxtMarkdownEditor.Text))
        {
            Clipboard.SetText(TxtMarkdownEditor.Text);
            MessageBox.Show("Nusxalandi!", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnSaveMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (TxtMarkdownEditor == null || string.IsNullOrWhiteSpace(TxtMarkdownEditor.Text))
        {
            MessageBox.Show("Markdown matni mavjud emas.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var defaultName = _activeResult != null
            ? $"{Path.GetFileNameWithoutExtension(_activeResult.FileName)}.md"
            : "document.md";

        var dlg = new SaveFileDialog
        {
            FileName = defaultName,
            DefaultExt = ".md",
            Filter = "Markdown Document (*.md)|*.md|Text Document (*.txt)|*.txt|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, TxtMarkdownEditor.Text, System.Text.Encoding.UTF8);
            MessageBox.Show($"Fayl saqlandi:\n{dlg.FileName}", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}