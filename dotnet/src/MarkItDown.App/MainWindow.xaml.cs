using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using MarkItDown.Core;
using MarkItDown.Core.Ai;
using MarkItDown.Core.Models;
using MarkItDown.Core.Ocr;
using Markdig;

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
    private readonly MarkdownPipeline _markdownPipeline;

    public ObservableCollection<ConversionResult> ConvertedItems { get; } = new();
    public ObservableCollection<PendingFileItem> SelectedFilesQueue { get; } = new();
    private ConversionResult? _activeResult;
    private bool _isInitializing = true;
    private string _currentLang = "uz";
    private string _currentContentType = "Obsidian"; // "PlainText" or "Obsidian"
    private string _currentViewMode = "Editor";      // "Editor", "Preview", "Split"

    public MainWindow()
    {
        SetBrowserEmulationMode();
        InitializeComponent();
        _engine = new MarkItDownEngine();
        _aiClient = new UniversalAiClient();
        _config = AppConfig.Load();

        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        LstConvertedItems.ItemsSource = ConvertedItems;
        LstSelectedFilesQueue.ItemsSource = SelectedFilesQueue;

        LoadSavedSettings();
        _isInitializing = false;
        LoadDefaultSample();
    }

    private void LoadSavedSettings()
    {
        // 1. AI Checkbox
        if (ChkEnableAi != null)
        {
            ChkEnableAi.IsChecked = _config.EnableAi;
        }

        // 2. Language
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

        // 3. Theme
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

        // 4. Provider
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

        // 5. Model
        if (CmbModelName != null && !string.IsNullOrEmpty(_config.SelectedModel))
        {
            CmbModelName.Text = _config.SelectedModel;
        }

        // 6. API Key
        var savedKey = _config.GetApiKey(_config.SelectedProvider);
        if (TxtApiKey != null)
        {
            TxtApiKey.Password = savedKey;
        }
        UpdateKeyStatusAndGuide(_config.SelectedProvider, savedKey);

        // 7. Custom URL
        if (TxtCustomBaseUrl != null)
        {
            TxtCustomBaseUrl.Text = _config.CustomBaseUrl ?? "http://localhost:11434";
        }
    }

    private void ChkEnableAi_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || ChkEnableAi == null) return;
        _config.EnableAi = ChkEnableAi.IsChecked == true;
        _config.Save();
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
                if (BtnViewPlainText != null) BtnViewPlainText.Content = "📝 Plain Text";
                if (BtnViewObsidian != null) BtnViewObsidian.Content = "🔮 Obsidian Preview";
                if (BtnViewSplit != null) BtnViewSplit.Content = "◫ Split";
                if (BtnViewEditor != null) BtnViewEditor.Content = "✏️ Editor";
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
                if (BtnViewPlainText != null) BtnViewPlainText.Content = "📝 Простой текст";
                if (BtnViewObsidian != null) BtnViewObsidian.Content = "🔮 Obsidian Preview";
                if (BtnViewSplit != null) BtnViewSplit.Content = "◫ Split";
                if (BtnViewEditor != null) BtnViewEditor.Content = "✏️ Редактор";
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
                if (BtnViewPlainText != null) BtnViewPlainText.Content = "📝 Oddiy Matn";
                if (BtnViewObsidian != null) BtnViewObsidian.Content = "🔮 Obsidian Preview";
                if (BtnViewSplit != null) BtnViewSplit.Content = "◫ Split";
                if (BtnViewEditor != null) BtnViewEditor.Content = "✏️ Editor";
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
- **Plain Text & Obsidian Modes:** Switch between clean Plain Text reading/editing and Rich Obsidian HTML preview!
",
            "ru" => @"# 📄 Добро пожаловать в MarkItDown Studio! 🚀

> 📌 **Документ:** `Руководство.md` | **Система:** Мульти-ИИ движок с авто-переключением и Windows OCR

## 1. Возможности
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Код и Изображения** в 100% чистый Markdown.
- **Очередь файлов:** Выберите файлы и нажмите **✨ Преобразовать файлы в Markdown (.md)**!
- **Простой текст и Obsidian:** Переключайтесь между режимом простого текста и богатым просмотром Obsidian!
",
            _ => @"# 📄 MarkItDown Studio ga Xush Kelibsiz! 🚀

> 📌 **Hujjat:** `Namuna_Qollanma.md` | **Tizim:** Multi-AI Smart Fallback Dvigatel & Windows OCR

## 1. Imkoniyatlar
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Kod va Rasmlar** 100% toza Markdown formatiga o'tkaziladi.
- **Fayllar Navbati:** Fayllarni tanlang, ro'yxatni ko'ring va **✨ Matnni Markdown (.md) ga O'tkazish** tugmasini bosing!
- **Oddiy Matn va Obsidian Rejimlari:** Oddiy toza o'qish matni yoki Obsidian boy HTML ko'rinishi o'rtasida bemalol almashing!
"
        };

        if (TxtMarkdownEditor != null)
        {
            TxtMarkdownEditor.Text = welcomeMd;
        }
        if (TxtPlainTextEditor != null)
        {
            TxtPlainTextEditor.Text = StripMarkdownToPlainText(welcomeMd);
        }
    }

    // ==========================================
    // 4 Dynamic View Modes: Plain Text, Obsidian, Split, Editor
    // ==========================================

    // 1. 📝 Oddiy Matn Preview (Plain Text Clean Reader)
    private void BtnViewPlainText_Click(object sender, RoutedEventArgs e)
    {
        _currentContentType = "PlainText";
        _currentViewMode = "Preview";

        ColEditor.Width = new GridLength(0);
        ColSplitter.Width = new GridLength(0);
        ColPreview.Width = new GridLength(1, GridUnitType.Star);

        EditorInnerBorder.Visibility = Visibility.Collapsed;
        ViewGridSplitter.Visibility = Visibility.Collapsed;
        PreviewInnerBorder.Visibility = Visibility.Visible;

        WebMarkdownPreview.Visibility = Visibility.Collapsed;
        TxtPlainTextPreview.Visibility = Visibility.Visible;

        // Render plain text
        TxtPlainTextPreview.Text = StripMarkdownToPlainText(TxtMarkdownEditor.Text);

        BtnViewEditor.IsEnabled = true;
        BtnViewEditor.Opacity = 1.0;

        BtnViewPlainText.Style = (Style)FindResource("AccentButton");
        BtnViewObsidian.Style = (Style)FindResource("GlassButton");
        BtnViewSplit.Style = (Style)FindResource("GlassButton");
        BtnViewEditor.Style = (Style)FindResource("GlassButton");
    }

    // 2. 🔮 Obsidian Preview (Rich Styled Markdown HTML)
    private void BtnViewObsidian_Click(object sender, RoutedEventArgs e)
    {
        _currentContentType = "Obsidian";
        _currentViewMode = "Preview";

        ColEditor.Width = new GridLength(0);
        ColSplitter.Width = new GridLength(0);
        ColPreview.Width = new GridLength(1, GridUnitType.Star);

        EditorInnerBorder.Visibility = Visibility.Collapsed;
        ViewGridSplitter.Visibility = Visibility.Collapsed;
        PreviewInnerBorder.Visibility = Visibility.Visible;

        WebMarkdownPreview.Visibility = Visibility.Visible;
        TxtPlainTextPreview.Visibility = Visibility.Collapsed;

        // Render Obsidian HTML
        UpdatePreviewHtml(TxtMarkdownEditor.Text);

        BtnViewEditor.IsEnabled = true;
        BtnViewEditor.Opacity = 1.0;

        BtnViewPlainText.Style = (Style)FindResource("GlassButton");
        BtnViewObsidian.Style = (Style)FindResource("AccentButton");
        BtnViewSplit.Style = (Style)FindResource("GlassButton");
        BtnViewEditor.Style = (Style)FindResource("GlassButton");
    }

    // 3. ◫ Split Mode (Editor on Left, Active Preview on Right, Editor Button Disabled)
    private void BtnViewSplit_Click(object sender, RoutedEventArgs e)
    {
        _currentViewMode = "Split";

        ColEditor.Width = new GridLength(1, GridUnitType.Star);
        ColSplitter.Width = GridLength.Auto;
        ColPreview.Width = new GridLength(1, GridUnitType.Star);

        EditorInnerBorder.Visibility = Visibility.Visible;
        ViewGridSplitter.Visibility = Visibility.Visible;
        PreviewInnerBorder.Visibility = Visibility.Visible;

        if (_currentContentType == "PlainText")
        {
            TxtMarkdownEditor.Visibility = Visibility.Collapsed;
            TxtPlainTextEditor.Visibility = Visibility.Visible;
            TxtPlainTextEditor.Text = StripMarkdownToPlainText(TxtMarkdownEditor.Text);

            WebMarkdownPreview.Visibility = Visibility.Collapsed;
            TxtPlainTextPreview.Visibility = Visibility.Visible;
            TxtPlainTextPreview.Text = TxtPlainTextEditor.Text;
        }
        else
        {
            TxtMarkdownEditor.Visibility = Visibility.Visible;
            TxtPlainTextEditor.Visibility = Visibility.Collapsed;

            WebMarkdownPreview.Visibility = Visibility.Visible;
            TxtPlainTextPreview.Visibility = Visibility.Collapsed;
            UpdatePreviewHtml(TxtMarkdownEditor.Text);
        }

        // Deactivate Editor button while Split is active
        BtnViewEditor.IsEnabled = false;
        BtnViewEditor.Opacity = 0.5;

        BtnViewPlainText.Style = (Style)FindResource("GlassButton");
        BtnViewObsidian.Style = (Style)FindResource("GlassButton");
        BtnViewSplit.Style = (Style)FindResource("AccentButton");
        BtnViewEditor.Style = (Style)FindResource("GlassButton");
    }

    // 4. ✏️ Editor Mode (Opens editor for the currently active content type)
    private void BtnViewEditor_Click(object sender, RoutedEventArgs e)
    {
        if (_currentViewMode == "Split") return; // Disabled during split

        _currentViewMode = "Editor";

        ColEditor.Width = new GridLength(1, GridUnitType.Star);
        ColSplitter.Width = new GridLength(0);
        ColPreview.Width = new GridLength(0);

        EditorInnerBorder.Visibility = Visibility.Visible;
        ViewGridSplitter.Visibility = Visibility.Collapsed;
        PreviewInnerBorder.Visibility = Visibility.Collapsed;

        if (_currentContentType == "PlainText")
        {
            TxtMarkdownEditor.Visibility = Visibility.Collapsed;
            TxtPlainTextEditor.Visibility = Visibility.Visible;
            TxtPlainTextEditor.Text = StripMarkdownToPlainText(TxtMarkdownEditor.Text);
        }
        else
        {
            TxtMarkdownEditor.Visibility = Visibility.Visible;
            TxtPlainTextEditor.Visibility = Visibility.Collapsed;
        }

        BtnViewEditor.IsEnabled = true;
        BtnViewEditor.Opacity = 1.0;

        BtnViewPlainText.Style = (Style)FindResource("GlassButton");
        BtnViewObsidian.Style = (Style)FindResource("GlassButton");
        BtnViewSplit.Style = (Style)FindResource("GlassButton");
        BtnViewEditor.Style = (Style)FindResource("AccentButton");
    }

    private void TxtMarkdownEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (_currentViewMode == "Split" || _currentViewMode == "Preview")
        {
            if (_currentContentType == "PlainText")
            {
                TxtPlainTextPreview.Text = StripMarkdownToPlainText(TxtMarkdownEditor.Text);
            }
            else
            {
                UpdatePreviewHtml(TxtMarkdownEditor.Text);
            }
        }
    }

    private void TxtPlainTextEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (_currentViewMode == "Split" && _currentContentType == "PlainText")
        {
            TxtPlainTextPreview.Text = TxtPlainTextEditor.Text;
        }
    }

    // Strip Markdown to pure clean Plain Text
    public static string StripMarkdownToPlainText(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return string.Empty;

        var text = markdown;
        text = Regex.Replace(text, @"```[\s\S]*?```", m =>
        {
            var code = m.Value;
            var lines = code.Split('\n');
            return string.Join("\n", lines.Skip(1).Take(Math.Max(0, lines.Length - 2)));
        });
        text = Regex.Replace(text, @"`([^`]+)`", "$1");
        text = Regex.Replace(text, @"!\[([^\]]*)\]\([^\)]+\)", "$1");
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        text = Regex.Replace(text, @"^#{1,6}\s*", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^>\s*", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "$1");
        text = Regex.Replace(text, @"\*([^*]+)\*", "$1");
        text = Regex.Replace(text, @"__([^_]+)__", "$1");
        text = Regex.Replace(text, @"_([^_]+)_", "$1");
        text = Regex.Replace(text, @"^---+\s*$", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"\|-+\|-+.*", "");
        text = Regex.Replace(text, @"\|", " ");

        return text.Trim();
    }

    private void UpdatePreviewHtml(string markdown)
    {
        try
        {
            if (WebMarkdownPreview == null) return;

            var htmlBody = Markdown.ToHtml(markdown ?? string.Empty, _markdownPipeline);
            var fullHtml = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge""/>
<style>
  body {{
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    background-color: #030712;
    color: #F8FAFC;
    padding: 24px;
    line-height: 1.65;
    font-size: 14px;
  }}
  h1, h2, h3, h4 {{ color: #818CF8; margin-top: 1.4em; margin-bottom: 0.5em; font-weight: 700; }}
  h1 {{ font-size: 22px; border-bottom: 1px solid #1E293B; padding-bottom: 8px; }}
  h2 {{ font-size: 18px; }}
  h3 {{ font-size: 15px; }}
  p {{ margin: 0.8em 0; }}
  table {{ border-collapse: collapse; width: 100%; margin: 16px 0; }}
  th, td {{ border: 1px solid #334155; padding: 8px 12px; text-align: left; }}
  th {{ background-color: #1E293B; color: #A5B4FC; font-weight: 600; }}
  tr:nth-child(even) {{ background-color: #0B1120; }}
  pre, code {{ background-color: #0F172A; color: #38BDF8; padding: 2px 6px; border-radius: 4px; font-family: Consolas, 'Cascadia Code', monospace; font-size: 12px; }}
  pre code {{ display: block; padding: 14px; overflow-x: auto; border: 1px solid #1E293B; }}
  blockquote {{ border-left: 4px solid #6366F1; margin: 12px 0; padding: 8px 14px; color: #94A3B8; background: #0F172A66; border-radius: 0 6px 6px 0; }}
  hr {{ border: 0; border-top: 1px solid #1E293B; margin: 24px 0; }}
  a {{ color: #818CF8; text-decoration: none; }}
  a:hover {{ text-decoration: underline; }}
  img {{ max-width: 100%; border-radius: 8px; margin: 12px 0; border: 1px solid #1E293B; }}
  ul, ol {{ padding-left: 24px; margin: 10px 0; }}
  li {{ margin: 4px 0; }}
</style>
</head>
<body>
{htmlBody}
</body>
</html>";
            WebMarkdownPreview.NavigateToString(fullHtml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Preview] Render xatosi: {ex.Message}");
        }
    }

    private static void SetBrowserEmulationMode()
    {
        try
        {
            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "MarkItDownStudio.exe");
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
            key?.SetValue(appName, 11001, Microsoft.Win32.RegistryValueKind.DWord);
            key?.SetValue("MarkItDown.App.exe", 11001, Microsoft.Win32.RegistryValueKind.DWord);
            key?.SetValue("MarkItDownStudio.exe", 11001, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch { }
    }

    // Tabs (1:1 with Web: Fayllar, Web URL, Matn / Bufer)
    private void BtnTabUpload_Click(object sender, RoutedEventArgs e)
    {
        PnlUploadMode.Visibility = Visibility.Visible;
        PnlUrlMode.Visibility = Visibility.Collapsed;
        PnlRawTextMode.Visibility = Visibility.Collapsed;
        BtnTabUpload.Style = (Style)FindResource("AccentButton");
        BtnTabUrl.Style = (Style)FindResource("GlassButton");
        BtnTabRawText.Style = (Style)FindResource("GlassButton");
    }

    private void BtnTabUrl_Click(object sender, RoutedEventArgs e)
    {
        PnlUploadMode.Visibility = Visibility.Collapsed;
        PnlUrlMode.Visibility = Visibility.Visible;
        PnlRawTextMode.Visibility = Visibility.Collapsed;
        BtnTabUpload.Style = (Style)FindResource("GlassButton");
        BtnTabUrl.Style = (Style)FindResource("AccentButton");
        BtnTabRawText.Style = (Style)FindResource("GlassButton");
    }

    private void BtnTabRawText_Click(object sender, RoutedEventArgs e)
    {
        PnlUploadMode.Visibility = Visibility.Collapsed;
        PnlUrlMode.Visibility = Visibility.Collapsed;
        PnlRawTextMode.Visibility = Visibility.Visible;
        BtnTabUpload.Style = (Style)FindResource("GlassButton");
        BtnTabUrl.Style = (Style)FindResource("GlassButton");
        BtnTabRawText.Style = (Style)FindResource("AccentButton");
    }

    private void BtnPasteFromClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                TxtRawInputText.Text = Clipboard.GetText();
            }
            else
            {
                MessageBox.Show("Buferda (Clipboard) matn topilmadi.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Buferdan olishda xatolik: {ex.Message}", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnConvertRawText_Click(object sender, RoutedEventArgs e)
    {
        var rawText = TxtRawInputText?.Text?.Trim();
        if (string.IsNullOrEmpty(rawText))
        {
            MessageBox.Show("Iltimos, avval formatlamoqchi bo'lgan matnni yozing yoki buferdan qo'ying.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var aiConfig = GetCurrentAiConfig();
        var isAiEnabled = (ChkEnableAi?.IsChecked == true);

        string finalMarkdown;
        string engineName;
        var usedAi = false;
        var tokensConsumed = 0;

        if (isAiEnabled && !string.IsNullOrWhiteSpace(aiConfig.ApiKey))
        {
            try
            {
                var prompt = @"Ushbu xom matnni (konspekt, kitob sahifasi, ma'ruza yoki qaydlar) Obsidian uchun to'liq boyitilgan, toza va mukammal Markdown formatiga o'tkazib ber.
Tegishli # Sarlavhalar, ## Kichik sarlavhalar, - Ro'yxatlar, |---| Jadvallar, > [!NOTE] iqtiboslar, **qalin** va *kursiv* urg'ular qo'sh. Ortiqcha so'z yoki tushuntirish qo'shma, faqat tayyor toza Markdown matnini qaytar.";

                var rawBytes = Encoding.UTF8.GetBytes(rawText);
                var aiRes = await _aiClient.ConvertWithAiAsync(rawBytes, "text/plain", "Raw_Notes.txt", aiConfig, prompt);
                finalMarkdown = aiRes.Markdown;
                tokensConsumed = aiRes.TokensConsumed;
                usedAi = true;
                engineName = $"{aiConfig.Provider} ({aiConfig.ModelName})";
            }
            catch
            {
                finalMarkdown = FormatRawTextToObsidianMarkdown(rawText);
                engineName = "Lokal Aqlli Obsidian Formatlagich";
            }
        }
        else
        {
            finalMarkdown = FormatRawTextToObsidianMarkdown(rawText);
            engineName = "Lokal Aqlli Obsidian Formatlagich";
        }

        sw.Stop();

        var title = ExtractTitle(rawText);
        var res = new ConversionResult
        {
            FileName = $"{title}.md",
            OriginalFormat = "Matn / Bufer",
            OriginalSizeBytes = Encoding.UTF8.GetByteCount(rawText),
            Markdown = finalMarkdown,
            WordCount = CountWords(finalMarkdown),
            CharCount = finalMarkdown.Length,
            LineCount = finalMarkdown.Split('\n').Length,
            EstimatedTokens = (int)Math.Ceiling(finalMarkdown.Length / 3.8),
            DurationMs = sw.ElapsedMilliseconds,
            UsedAi = usedAi,
            TokensConsumed = tokensConsumed,
            EngineName = engineName,
            IsSuccess = true
        };

        ConvertedItems.Insert(0, res);
        SetActiveResult(res);

        // Switch to Obsidian Preview
        BtnViewObsidian_Click(this, new RoutedEventArgs());
    }

    public static string FormatRawTextToObsidianMarkdown(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

        var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();

        var title = ExtractTitle(rawText);
        sb.AppendLine($"# 📄 {title}");
        sb.AppendLine();
        sb.AppendLine($"> 📌 **Format:** Obsidian Markdown | **Vaqt:** {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        var inTable = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                if (inTable) inTable = false;
                sb.AppendLine();
                continue;
            }

            // Headings detection (ALL CAPS or short titles or numbers like "МУНДАРИЖА", "1. БОБ")
            if (Regex.IsMatch(line, @"^(МУНДАРИЖА|ТАДБИР|МУҚАДДИМА|ХОТИМА|БОБ\s*\d+|ФАСЛ\s*\d+|[\dIVXLCDM]+\.\s*[A-ZА-ЯЁҚҒҲЎ])", RegexOptions.IgnoreCase) ||
                (line.Length < 60 && line == line.ToUpperInvariant() && line.Count(char.IsLetter) > 3))
            {
                if (inTable) inTable = false;
                sb.AppendLine();
                sb.AppendLine($"## 📌 {line}");
                sb.AppendLine();
                continue;
            }

            // Dotted or numbered table of contents / index lines (e.g. "Title . . . 14" or "Title 14")
            var tocMatch = Regex.Match(line, @"^(.*?)(?:\.{2,}|\s{3,})(\d+)$");
            if (tocMatch.Success)
            {
                var itemTitle = tocMatch.Groups[1].Value.Trim().TrimEnd('.', '-');
                var pageNum = tocMatch.Groups[2].Value.Trim();
                sb.AppendLine($"- **{itemTitle}** `[Sahifa: {pageNum}]`");
                continue;
            }

            // Bullet lists detection
            if (Regex.IsMatch(line, @"^[\d]+[\.\)]\s+"))
            {
                sb.AppendLine(line);
                continue;
            }
            if (Regex.IsMatch(line, @"^[-*•]\s+"))
            {
                sb.AppendLine($"- {line.Substring(1).Trim()}");
                continue;
            }

            // Important medical/rule alerts detection
            if (Regex.IsMatch(line, @"^(эслатма|диққат|муҳим|қоида|note|important):", RegexOptions.IgnoreCase))
            {
                sb.AppendLine();
                sb.AppendLine("> [!IMPORTANT]");
                sb.AppendLine($"> **{line}**");
                sb.AppendLine();
                continue;
            }

            // Default paragraph line
            sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }

    private static string ExtractTitle(string text)
    {
        var firstLine = text.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => !string.IsNullOrEmpty(l));
        if (!string.IsNullOrEmpty(firstLine))
        {
            var clean = Regex.Replace(firstLine, @"[^\w\s-]", "").Trim();
            if (clean.Length > 30) clean = clean.Substring(0, 30);
            if (!string.IsNullOrEmpty(clean)) return clean;
        }
        return $"Hujjat_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // ==========================================
    // Modals with HWND Airspace Fix (prevents freeze!)
    // ==========================================
    private void BtnOpenApiKey_Click(object sender, RoutedEventArgs e)
    {
        // Temporarily hide native Win32 WebBrowser control to prevent WPF airspace capture
        if (PreviewInnerBorder != null) PreviewInnerBorder.Visibility = Visibility.Collapsed;
        PnlApiKeyModal.Visibility = Visibility.Visible;
    }

    private void BtnCloseApiKeyModal_Click(object sender, RoutedEventArgs e)
    {
        PnlApiKeyModal.Visibility = Visibility.Collapsed;

        // Restore preview visibility if in Preview or Split mode
        if (_currentViewMode == "Preview" || _currentViewMode == "Split")
        {
            if (PreviewInnerBorder != null) PreviewInnerBorder.Visibility = Visibility.Visible;
        }

        _config.SelectedProvider = GetSelectedProvider();
        _config.SelectedModel = CmbModelName?.Text?.Trim() ?? "gemini-2.5-flash";
        _config.SetApiKey(_config.SelectedProvider, TxtApiKey?.Password?.Trim() ?? string.Empty);
        _config.CustomBaseUrl = TxtCustomBaseUrl?.Text?.Trim();
        _config.Save();
        UpdateKeyStatusAndGuide(_config.SelectedProvider, TxtApiKey?.Password?.Trim() ?? string.Empty);
    }

    private void BtnOpenFormats_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewInnerBorder != null) PreviewInnerBorder.Visibility = Visibility.Collapsed;
        PnlFormatsModal.Visibility = Visibility.Visible;
    }

    private void BtnCloseFormatsModal_Click(object sender, RoutedEventArgs e)
    {
        PnlFormatsModal.Visibility = Visibility.Collapsed;
        if (_currentViewMode == "Preview" || _currentViewMode == "Split")
        {
            if (PreviewInnerBorder != null) PreviewInnerBorder.Visibility = Visibility.Visible;
        }
    }

    // Window KeyDown (Ctrl+V paste image/text)
    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (TxtMarkdownEditor.IsFocused || TxtPlainTextEditor.IsFocused || TxtWebUrl.IsFocused || TxtApiKey.IsFocused) return;

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
                PnlCustomBaseUrl.Visibility = (provider == AiProvider.OllamaLocal || provider == AiProvider.OllamaCloud || provider == AiProvider.CustomOpenAICompatible)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                if (provider == AiProvider.OllamaLocal && (string.IsNullOrWhiteSpace(TxtCustomBaseUrl.Text) || TxtCustomBaseUrl.Text.Contains("8000")))
                {
                    TxtCustomBaseUrl.Text = "http://localhost:11434";
                }
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

        var models = _config.GetModelsForProvider(provider);
        foreach (var model in models)
        {
            CmbModelName.Items.Add(model);
        }

        if (CmbModelName.Items.Count > 0)
        {
            CmbModelName.SelectedIndex = 0;
        }
    }

    private void BtnAddCustomModel_Click(object sender, RoutedEventArgs e)
    {
        var newModel = TxtNewCustomModel?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(newModel))
        {
            MessageBox.Show("Iltimos, qo'shmoqchi bo'lgan model nomini yozing.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var provider = GetSelectedProvider();
        _config.AddCustomModel(provider, newModel);
        if (TxtNewCustomModel != null) TxtNewCustomModel.Text = string.Empty;

        PopulateModelNames(provider);
        CmbModelName.Text = newModel;
        _config.SelectedModel = newModel;
        _config.Save();

        MessageBox.Show($"\"{newModel}\" modeli {provider} ro'yxatiga muvaffaqiyatli qo'shildi!", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnDeleteCustomModel_Click(object sender, RoutedEventArgs e)
    {
        var currentModel = CmbModelName?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(currentModel)) return;

        var provider = GetSelectedProvider();
        _config.RemoveCustomModel(provider, currentModel);
        PopulateModelNames(provider);

        MessageBox.Show($"\"{currentModel}\" modeli {provider} ro'yxatidan o'chirildi.", "O'chirildi", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void TxtApiKey_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || TxtApiKey == null) return;
        var key = TxtApiKey.Password?.Trim() ?? string.Empty;
        var provider = GetSelectedProvider();
        UpdateKeyStatusAndGuide(provider, key);
    }

    private void CmbModelName_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || CmbModelName == null) return;
        var custom = CmbModelName.Text?.Trim();
        if (!string.IsNullOrEmpty(custom))
        {
            _config.SelectedModel = custom;
            _config.Save();
        }
    }

    private void TxtCustomBaseUrl_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || TxtCustomBaseUrl == null) return;
        _config.CustomBaseUrl = TxtCustomBaseUrl.Text?.Trim();
        _config.Save();
    }

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
            EnableAi = ChkEnableAi?.IsChecked == true,
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
                // Graceful fallback to offline engine on API errors without crash
                if (options.EnableAi)
                {
                    try
                    {
                        var offlineOptions = new ConversionOptions { EnableAi = false, AutoOcrScannedPdf = true };
                        var fallbackResults = await _engine.ConvertFileAsync(path, offlineOptions, null);
                        foreach (var result in fallbackResults)
                        {
                            ConvertedItems.Insert(0, result);
                            SetActiveResult(result);
                        }
                        MessageBox.Show($"AI API xatolik berdi ({ex.Message}).\nFayl Oflayn Dvigatel orqali muvaffaqiyatli o'girildi!", "Ogohlantirish", MessageBoxButton.OK, MessageBoxImage.Information);
                        continue;
                    }
                    catch { }
                }

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
        if (TxtPlainTextEditor != null) TxtPlainTextEditor.Text = StripMarkdownToPlainText(result.Markdown);
        if (TxtPlainTextPreview != null) TxtPlainTextPreview.Text = StripMarkdownToPlainText(result.Markdown);

        UpdatePreviewHtml(result.Markdown);
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
        if (aiConfig.Provider != AiProvider.OllamaLocal && string.IsNullOrWhiteSpace(aiConfig.ApiKey))
        {
            if (PreviewInnerBorder != null) PreviewInnerBorder.Visibility = Visibility.Collapsed;
            PnlApiKeyModal.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var prompt = @"Ushbu Markdown hujjatidagi barcha orfografik, grammatik xatoliklarni, buzilgan jadvallarni va Lotin/Krill chalkashliklarini (o'/ў, g'/ғ, sh/ш, ch/ч) to'liq to'g'rilab, toza va chiroyli Markdown qilib qaytar. Ortiqcha izohlarsiz, faqat to'g'rilangan yakuniy Markdownni ber.";
            var rawBytes = System.Text.Encoding.UTF8.GetBytes(currentText);

            var (correctedMd, _) = await _aiClient.ConvertWithAiAsync(rawBytes, "text/plain", "document_review.md", aiConfig, prompt);

            if (TxtAiProofreadResult != null) TxtAiProofreadResult.Text = correctedMd;

            if (PreviewInnerBorder != null) PreviewInnerBorder.Visibility = Visibility.Collapsed;
            if (PnlAiReviewModal != null) PnlAiReviewModal.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"AI tahlilida xatolik yuz berdi ({ex.Message}). API kalitini tekshirib ko'ring.", "AI Xatoligi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnCloseReviewModal_Click(object sender, RoutedEventArgs e)
    {
        if (PnlAiReviewModal != null) PnlAiReviewModal.Visibility = Visibility.Collapsed;
        if (_currentViewMode == "Preview" || _currentViewMode == "Split")
        {
            if (PreviewInnerBorder != null) PreviewInnerBorder.Visibility = Visibility.Visible;
        }
    }

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
            if (_currentViewMode == "Preview" || _currentViewMode == "Split")
            {
                if (PreviewInnerBorder != null) PreviewInnerBorder.Visibility = Visibility.Visible;
            }
            MessageBox.Show("Markdown hujjati AI tuzatishlari bilan yangilandi!", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnCopyMarkdown_Click(object sender, RoutedEventArgs e)
    {
        var textToCopy = _currentContentType == "PlainText" ? TxtPlainTextEditor.Text : TxtMarkdownEditor.Text;
        if (!string.IsNullOrEmpty(textToCopy))
        {
            Clipboard.SetText(textToCopy);
            MessageBox.Show("Nusxalandi!", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnSaveMarkdown_Click(object sender, RoutedEventArgs e)
    {
        var textToSave = _currentContentType == "PlainText" ? TxtPlainTextEditor.Text : TxtMarkdownEditor.Text;
        if (string.IsNullOrWhiteSpace(textToSave))
        {
            MessageBox.Show("Matn mavjud emas.", "Xabar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var isPlain = _currentContentType == "PlainText";
        var defaultExt = isPlain ? ".txt" : ".md";
        var defaultName = _activeResult != null
            ? $"{Path.GetFileNameWithoutExtension(_activeResult.FileName)}{defaultExt}"
            : $"document{defaultExt}";

        var dlg = new SaveFileDialog
        {
            FileName = defaultName,
            DefaultExt = defaultExt,
            Filter = isPlain
                ? "Text Document (*.txt)|*.txt|Markdown Document (*.md)|*.md|All Files (*.*)|*.*"
                : "Markdown Document (*.md)|*.md|Text Document (*.txt)|*.txt|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, textToSave, System.Text.Encoding.UTF8);
            MessageBox.Show($"Fayl saqlandi:\n{dlg.FileName}", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}