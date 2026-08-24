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

namespace MarkItDown.App;

public partial class MainWindow : Window
{
    private readonly MarkItDownEngine _engine;
    private readonly UniversalAiClient _aiClient;
    private readonly AppConfig _config;
    public ObservableCollection<ConversionResult> ConvertedItems { get; } = new();
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

        // 6. Custom URL & AI switch
        if (TxtCustomBaseUrl != null)
        {
            TxtCustomBaseUrl.Text = _config.CustomBaseUrl ?? "http://localhost:11434";
        }
        if (ChkEnableAi != null)
        {
            ChkEnableAi.IsChecked = _config.EnableAi;
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
                if (LblLanguage != null) LblLanguage.Text = "🌐 Lang:";
                if (LblTheme != null) LblTheme.Text = "🎨 Theme:";
                if (LblAiProvider != null) LblAiProvider.Text = "AI Provider:";
                if (LblModel != null) LblModel.Text = "Model:";
                if (LblApiKey != null) LblApiKey.Text = "API Key:";
                if (TxtDropTitle != null) TxtDropTitle.Text = "Drop your file here";
                if (TxtDropSubtitle != null) TxtDropSubtitle.Text = "PDF, Word, Excel, PPTX, Images (OCR), Audio, ZIP, Code";
                if (BtnSelectFile != null) BtnSelectFile.Content = "Select File...";
                if (BtnConvertUrl != null) BtnConvertUrl.Content = "Convert URL";
                if (TxtHistoryTitle != null) TxtHistoryTitle.Text = "Converted Documents";
                if (BtnClearAll != null) BtnClearAll.Content = "🗑️ Clear All";
                if (BtnAiProofread != null) BtnAiProofread.Content = "✨ AI Proofread";
                if (BtnCopyMarkdown != null) BtnCopyMarkdown.Content = "📋 Copy";
                if (BtnSaveMarkdown != null) BtnSaveMarkdown.Content = "💾 Save .md";
                if (TxtStatus != null) TxtStatus.Text = "Ready. Upload PDF, Word, Excel, PPTX, Image or Audio.";
                if (TxtFooterTagline != null) TxtFooterTagline.Text = "Multi-AI Auto-Fallback Architecture • 100% C# .NET 10";
                if (TxtModalHeader != null) TxtModalHeader.Text = "AI Analysis & Error Correction Result";
                if (BtnModalCancel != null) BtnModalCancel.Content = "Cancel";
                if (BtnModalApply != null) BtnModalApply.Content = "✅ Apply to Document";
                break;

            case "ru":
                if (LblLanguage != null) LblLanguage.Text = "🌐 Язык:";
                if (LblTheme != null) LblTheme.Text = "🎨 Тема:";
                if (LblAiProvider != null) LblAiProvider.Text = "ИИ Провайдер:";
                if (LblModel != null) LblModel.Text = "Модель:";
                if (LblApiKey != null) LblApiKey.Text = "API Ключ:";
                if (TxtDropTitle != null) TxtDropTitle.Text = "Перетащите файл сюда";
                if (TxtDropSubtitle != null) TxtDropSubtitle.Text = "PDF, Word, Excel, PPTX, Изображения (OCR), Аудио, ZIP, Код";
                if (BtnSelectFile != null) BtnSelectFile.Content = "Выбрать файл...";
                if (BtnConvertUrl != null) BtnConvertUrl.Content = "Конвертировать URL";
                if (TxtHistoryTitle != null) TxtHistoryTitle.Text = "Конвертированные файлы";
                if (BtnClearAll != null) BtnClearAll.Content = "🗑️ Очистить всё";
                if (BtnAiProofread != null) BtnAiProofread.Content = "✨ Проверить с ИИ";
                if (BtnCopyMarkdown != null) BtnCopyMarkdown.Content = "📋 Копировать";
                if (BtnSaveMarkdown != null) BtnSaveMarkdown.Content = "💾 Сохранить .md";
                if (TxtStatus != null) TxtStatus.Text = "Готово. Загрузите PDF, Word, Excel, PPTX, изображение или аудио.";
                if (TxtFooterTagline != null) TxtFooterTagline.Text = "Мульти-ИИ архитектура с авто-переключением • 100% C# .NET 10";
                if (TxtModalHeader != null) TxtModalHeader.Text = "Результат анализа и исправления ИИ";
                if (BtnModalCancel != null) BtnModalCancel.Content = "Отмена";
                if (BtnModalApply != null) BtnModalApply.Content = "✅ Применить к документу";
                break;

            default: // uz
                if (LblLanguage != null) LblLanguage.Text = "🌐 Til:";
                if (LblTheme != null) LblTheme.Text = "🎨 Mavzu:";
                if (LblAiProvider != null) LblAiProvider.Text = "AI Provayder:";
                if (LblModel != null) LblModel.Text = "Model:";
                if (LblApiKey != null) LblApiKey.Text = "API Kalit:";
                if (TxtDropTitle != null) TxtDropTitle.Text = "Faylni bu yerga tashlang";
                if (TxtDropSubtitle != null) TxtDropSubtitle.Text = "PDF, Word, Excel, PPTX, Rasm (OCR), Audio, ZIP, Kod";
                if (BtnSelectFile != null) BtnSelectFile.Content = "Fayl Tanlash...";
                if (BtnConvertUrl != null) BtnConvertUrl.Content = "URL O'girish";
                if (TxtHistoryTitle != null) TxtHistoryTitle.Text = "O'girilgan Hujjatlar";
                if (BtnClearAll != null) BtnClearAll.Content = "🗑️ Barchasini Tozalash";
                if (BtnAiProofread != null) BtnAiProofread.Content = "✨ AI Bilan Tekshirish";
                if (BtnCopyMarkdown != null) BtnCopyMarkdown.Content = "📋 Nusxalash";
                if (BtnSaveMarkdown != null) BtnSaveMarkdown.Content = "💾 .md Saqlash";
                if (TxtStatus != null) TxtStatus.Text = "Tayyor. PDF, Word, Excel, PPTX, Rasm yoki Audio yuklang.";
                if (TxtFooterTagline != null) TxtFooterTagline.Text = "Multi-AI Auto-Fallback Architecture • 100% C# .NET 10";
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
            "en" => @"# 📄 Welcome to MarkItDown Studio .NET! 🚀

> 📌 **Document:** `Sample_Guide.md` | **System:** Multi-AI Smart Fallback Engine

## 1. Capabilities
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Code and ZIP** converted into 100% clean Markdown.
- No unwanted clutter, original document flow preserved.

## 2. Universal AI Providers & Models
- **Google Gemini:** `gemini-2.5-flash`, `gemini-2.5-pro`, `gemini-3.7-flash`, `gemini-3-flash`, `gemini-3-pro`
- **Groq AI (Ultra-Fast 500+ tok/s):** `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`, `deepseek-r1-distill-llama-70b`
- **OpenAI:** `gpt-4o`, `gpt-4o-mini`, `o3-mini`, `o1`
- **Anthropic Claude:** `claude-3-7-sonnet`, `claude-3-5-sonnet`, `claude-3-5-haiku`
- **DeepSeek:** `deepseek-chat` (V3), `deepseek-reasoner` (R1)
- **Ollama:** `llama3.2-vision`, `llava`, `qwen2.5-vl`

## 3. Smart Fallback
- If the selected model hits a rate limit or error, it seamlessly fails over to backup models without interrupting your workflow!
",
            "ru" => @"# 📄 Добро пожаловать в MarkItDown Studio .NET! 🚀

> 📌 **Документ:** `Руководство.md` | **Система:** Мульти-ИИ движок с авто-переключением

## 1. Возможности
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Код и ZIP** в 100% чистый Markdown.
- Без лишнего мусора, с сохранением структуры оригинального документа.

## 2. Универсальные ИИ Провайдеры и Модели
- **Google Gemini:** `gemini-2.5-flash`, `gemini-2.5-pro`, `gemini-3.7-flash`, `gemini-3-flash`, `gemini-3-pro`
- **Groq AI (Сверхбыстрый 500+ ток/сек):** `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`, `deepseek-r1-distill-llama-70b`
- **OpenAI:** `gpt-4o`, `gpt-4o-mini`, `o3-mini`, `o1`
- **Anthropic Claude:** `claude-3-7-sonnet`, `claude-3-5-sonnet`, `claude-3-5-haiku`
- **DeepSeek:** `deepseek-chat` (V3), `deepseek-reasoner` (R1)
- **Ollama:** `llama3.2-vision`, `llava`, `qwen2.5-vl`

## 3. Умное авто-переключение (Fallback)
- Если у выбранной модели исчерпан лимит запросов, система автоматически переключится на резервную модель без остановки работы!
",
            _ => @"# 📄 MarkItDown Studio .NET ga Xush Kelibsiz! 🚀

> 📌 **Hujjat:** `Namuna_Qollanma.md` | **Tizim:** Multi-AI Smart Fallback Dvigatel

## 1. Imkoniyatlar
- **PDF, Word (.docx), Excel (.xlsx), PowerPoint (.pptx), CSV, JSON, HTML, Kod va ZIP** fayllarni 100% toza matnga o'girish.
- Hech qanday ortiqcha axlat matnlarsiz, hujjatdagi asl tartib saqlanadi.

## 2. Universal AI Provayderlar & Modellar
- **Google Gemini:** `gemini-2.5-flash`, `gemini-2.5-pro`, `gemini-3.7-flash`, `gemini-3-flash`, `gemini-3-pro`, `gemini-3-deep-think`
- **Groq AI (Ultra-Tez 500+ tok/s):** `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`, `deepseek-r1-distill-llama-70b`
- **OpenAI:** `gpt-4o`, `gpt-4o-mini`, `o3-mini`, `o1`
- **Anthropic Claude:** `claude-3-7-sonnet`, `claude-3-5-sonnet`, `claude-3-5-haiku`
- **DeepSeek:** `deepseek-chat` (V3), `deepseek-reasoner` (R1)
- **Ollama:** `llama3.2-vision`, `llava`, `qwen2.5-vl`

## 3. Smart Fallback (Avtomatik o'tish)
- Agar tanlangan modelning limiti tugasa yoki band bo'lsa, tizim avtomatik zaxira modelga o'tib, konvertatsiyani to'xtovsiz davom ettiradi!
"
        };

        if (TxtMarkdownEditor != null)
        {
            TxtMarkdownEditor.Text = welcomeMd;
        }
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

    // Theme Switcher & Modal Adaptation
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
        if (MainBorder == null || TitleBarBorder == null || DropArea == null || EditorContainerBorder == null)
            return;

        switch (theme)
        {
            case "ObsidianDark":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(28, 28, 28));
                if (SettingsBarBorder != null) SettingsBarBorder.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(168, 85, 247));
                if (DropIconBadge != null) DropIconBadge.Background = new SolidColorBrush(Color.FromRgb(88, 28, 135));
                if (LogoBadge != null) LogoBadge.Background = new SolidColorBrush(Color.FromRgb(168, 85, 247));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                if (EditorInnerBorder != null) EditorInnerBorder.Background = new SolidColorBrush(Color.FromRgb(16, 16, 16));
                if (FooterBorder != null) FooterBorder.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                if (ModalDialogBorder != null)
                {
                    ModalDialogBorder.Background = new SolidColorBrush(Color.FromRgb(28, 28, 28));
                    ModalDialogBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(168, 85, 247));
                }
                break;

            case "CyberpunkNeon":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(5, 8, 17));
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(10, 15, 30));
                if (SettingsBarBorder != null) SettingsBarBorder.Background = new SolidColorBrush(Color.FromRgb(8, 12, 24));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                if (DropIconBadge != null) DropIconBadge.Background = new SolidColorBrush(Color.FromRgb(8, 145, 178));
                if (LogoBadge != null) LogoBadge.Background = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                if (EditorInnerBorder != null) EditorInnerBorder.Background = new SolidColorBrush(Color.FromRgb(2, 6, 23));
                if (FooterBorder != null) FooterBorder.Background = new SolidColorBrush(Color.FromRgb(5, 8, 17));
                if (ModalDialogBorder != null)
                {
                    ModalDialogBorder.Background = new SolidColorBrush(Color.FromRgb(10, 15, 30));
                    ModalDialogBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                }
                break;

            case "FrostedCrystal":
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                if (SettingsBarBorder != null) SettingsBarBorder.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                if (DropIconBadge != null) DropIconBadge.Background = new SolidColorBrush(Color.FromRgb(219, 234, 254));
                if (LogoBadge != null) LogoBadge.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                if (EditorInnerBorder != null) EditorInnerBorder.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                if (FooterBorder != null) FooterBorder.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                if (TxtAppTitle != null) TxtAppTitle.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                if (TxtDocTitle != null) TxtDocTitle.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                if (TxtMarkdownEditor != null) TxtMarkdownEditor.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                if (ModalDialogBorder != null)
                {
                    ModalDialogBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    ModalDialogBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                }
                break;

            default: // MidnightGlass
                MainBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                if (SettingsBarBorder != null) SettingsBarBorder.Background = new SolidColorBrush(Color.FromRgb(22, 32, 50));
                DropArea.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                if (DropIconBadge != null) DropIconBadge.Background = new SolidColorBrush(Color.FromRgb(49, 46, 129));
                if (LogoBadge != null) LogoBadge.Background = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                EditorContainerBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                if (EditorInnerBorder != null) EditorInnerBorder.Background = new SolidColorBrush(Color.FromRgb(11, 17, 32));
                if (FooterBorder != null) FooterBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                if (TxtAppTitle != null) TxtAppTitle.Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                if (TxtDocTitle != null) TxtDocTitle.Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                if (TxtMarkdownEditor != null) TxtMarkdownEditor.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                if (ModalDialogBorder != null)
                {
                    ModalDialogBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                    ModalDialogBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                }
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
            if (TxtApiKey != null)
            {
                TxtApiKey.Password = key;
            }
            UpdateKeyStatusAndGuide(provider, key);

            _config.SelectedProvider = provider;
            _config.Save();
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
            if (CmbModelName.Items.Count > 0)
            {
                CmbModelName.SelectedIndex = 0;
            }
        }
    }

    private void TxtApiKey_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || TxtApiKey == null) return;
        var key = TxtApiKey.Password?.Trim() ?? string.Empty;
        var provider = GetSelectedProvider();
        _config.SetApiKey(provider, key);
        UpdateKeyStatusAndGuide(provider, key);
    }

    private void CmbModelName_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || CmbModelName == null) return;
        _config.SelectedModel = CmbModelName.Text?.Trim() ?? "gemini-2.5-flash";
        _config.Save();
    }

    private void TxtCustomBaseUrl_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || TxtCustomBaseUrl == null) return;
        _config.CustomBaseUrl = TxtCustomBaseUrl.Text?.Trim();
        _config.Save();
    }

    private void ChkEnableAi_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || ChkEnableAi == null) return;
        _config.EnableAi = ChkEnableAi.IsChecked == true;
        _config.Save();
    }

    private void UpdateKeyStatusAndGuide(AiProvider provider, string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            if (TxtKeyStatus != null)
            {
                TxtKeyStatus.Text = _currentLang switch
                {
                    "en" => "🔒 Key saved",
                    "ru" => "🔒 Ключ сохранен",
                    _ => "🔒 Kalit saqlandi"
                };
                TxtKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            if (PnlKeyGuide != null) PnlKeyGuide.Visibility = Visibility.Collapsed;
        }
        else
        {
            if (TxtKeyStatus != null)
            {
                TxtKeyStatus.Text = _currentLang switch
                {
                    "en" => "⚠️ Key missing",
                    "ru" => "⚠️ Ключ не введен",
                    _ => "⚠️ Kalit kiritilmagan"
                };
                TxtKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
            if (PnlKeyGuide != null && TxtKeyGuide != null)
            {
                PnlKeyGuide.Visibility = Visibility.Visible;
                if (AiProviderConfig.ProviderGuide.TryGetValue(provider, out var guide))
                {
                    TxtKeyGuide.Text = guide;
                }
            }
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

    // Drag and Drop
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
            DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
        }
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
        var filterText = _currentLang switch
        {
            "en" => "All Supported Files|*.pdf;*.docx;*.doc;*.pptx;*.ppt;*.xlsx;*.xls;*.ods;*.csv;*.tsv;*.json;*.html;*.htm;*.txt;*.py;*.cs;*.js;*.ts;*.png;*.jpg;*.jpeg;*.webp;*.mp3;*.wav;*.m4a;*.zip|Documents (*.pdf, *.docx, *.pptx, *.xlsx)|*.pdf;*.docx;*.pptx;*.xlsx|All Files (*.*)|*.*",
            "ru" => "Все поддерживаемые файлы|*.pdf;*.docx;*.doc;*.pptx;*.ppt;*.xlsx;*.xls;*.ods;*.csv;*.tsv;*.json;*.html;*.htm;*.txt;*.py;*.cs;*.js;*.ts;*.png;*.jpg;*.jpeg;*.webp;*.mp3;*.wav;*.m4a;*.zip|Документы (*.pdf, *.docx, *.pptx, *.xlsx)|*.pdf;*.docx;*.pptx;*.xlsx|Все файлы (*.*)|*.*",
            _ => "Barcha Qo'llab-quvvatlanuvchi Fayllar|*.pdf;*.docx;*.doc;*.pptx;*.ppt;*.xlsx;*.xls;*.ods;*.csv;*.tsv;*.json;*.html;*.htm;*.txt;*.py;*.cs;*.js;*.ts;*.png;*.jpg;*.jpeg;*.webp;*.mp3;*.wav;*.m4a;*.zip|Hujjatlar (*.pdf, *.docx, *.pptx, *.xlsx)|*.pdf;*.docx;*.pptx;*.xlsx|Barcha Fayllar (*.*)|*.*"
        };

        var dlg = new OpenFileDialog
        {
            Title = _currentLang switch { "en" => "Select files to convert", "ru" => "Выберите файлы для конвертации", _ => "Konvertatsiya uchun fayllarni tanlang" },
            Multiselect = true,
            Filter = filterText
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

        if (TxtStatus != null) TxtStatus.Text = $"{filePaths.Length} files converting...";

        foreach (var path in filePaths)
        {
            try
            {
                var fileName = Path.GetFileName(path);
                if (TxtStatus != null) TxtStatus.Text = $"Processing: {fileName}...";

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

        if (TxtStatus != null) TxtStatus.Text = _currentLang switch { "en" => "All files converted successfully!", "ru" => "Все файлы успешно конвертированы!", _ => "Barcha fayllar muvaffaqiyatli o'girildi!" };
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

        if (TxtStatus != null) TxtStatus.Text = $"Loading URL: {url}...";
        try
        {
            var options = GetCurrentOptions();
            var aiConfig = GetCurrentAiConfig();

            var result = await _engine.ConvertUrlAsync(url, options, aiConfig);
            ConvertedItems.Insert(0, result);
            SetActiveResult(result);
            if (TxtStatus != null) TxtStatus.Text = "URL converted!";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"URL error:\n{ex.Message}", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
            if (TxtStatus != null) TxtStatus.Text = "Error.";
        }
    }

    private void SetActiveResult(ConversionResult result)
    {
        _activeResult = result;
        if (TxtDocTitle != null) TxtDocTitle.Text = result.FileName;
        if (TxtDocStats != null) TxtDocStats.Text = $"Format: {result.OriginalFormat} • {result.WordCount:N0} {_currentLang switch { "en" => "words", "ru" => "слов", _ => "ta so'z" }} • {result.CharCount:N0} {_currentLang switch { "en" => "chars", "ru" => "симв.", _ => "ta belgi" }} • {result.DurationMs} ms • Dvigatel: {result.EngineName}";
        if (TxtMarkdownEditor != null) TxtMarkdownEditor.Text = result.Markdown;
    }

    private void LstConvertedItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstConvertedItems.SelectedItem is ConversionResult res)
        {
            SetActiveResult(res);
        }
    }

    // 1. Delete Single Item from History
    private void BtnDeleteSingleItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ConversionResult item)
        {
            ConvertedItems.Remove(item);
            if (_activeResult == item)
            {
                if (ConvertedItems.Count > 0)
                {
                    SetActiveResult(ConvertedItems[0]);
                }
                else
                {
                    LoadDefaultSample();
                }
            }
            if (TxtStatus != null) TxtStatus.Text = $"\"{item.FileName}\" removed.";
        }
    }

    // 2. Clear All History
    private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
    {
        if (ConvertedItems.Count == 0) return;
        var msg = _currentLang switch
        {
            "en" => "Are you sure you want to clear all converted documents history?",
            "ru" => "Вы уверены, что хотите очистить всю историю файлов?",
            _ => "Haqiqatan ham barcha o'girilgan fayllar tarixini tozalamoqchimisiz?"
        };

        var title = _currentLang switch { "en" => "Clear History", "ru" => "Очистка истории", _ => "Tarixni tozalash" };

        var res = MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            ConvertedItems.Clear();
            LoadDefaultSample();
            if (TxtStatus != null) TxtStatus.Text = "Cleared.";
        }
    }

    // 3. AI Proofreader / Error Fixer
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
            MessageBox.Show("API Key talab qilinadi.", "API Key", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TxtStatus != null) TxtStatus.Text = "AI Proofreading...";
        try
        {
            var prompt = @"Ushbu Markdown hujjatidagi barcha orfografik, grammatik xatoliklarni, buzilgan jadvallarni va Lotin/Krill chalkashliklarini (o'/ў, g'/ғ, sh/ш, ch/ч) to'liq to'g'rilab, toza va chiroyli Markdown qilib qaytar. Ortiqcha izohlarsiz, faqat to'g'rilangan yakuniy Markdownni ber.";
            var rawBytes = System.Text.Encoding.UTF8.GetBytes(currentText);

            var (correctedMd, _) = await _aiClient.ConvertWithAiAsync(rawBytes, "text/plain", "document_review.md", aiConfig, prompt);

            if (TxtAiProofreadResult != null) TxtAiProofreadResult.Text = correctedMd;
            if (PnlAiReviewModal != null) PnlAiReviewModal.Visibility = Visibility.Visible;
            if (TxtStatus != null) TxtStatus.Text = "Proofreading complete!";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"AI error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            if (TxtStatus != null) TxtStatus.Text = "Error.";
        }
    }

    private void BtnCloseReviewModal_Click(object sender, RoutedEventArgs e)
    {
        if (PnlAiReviewModal != null) PnlAiReviewModal.Visibility = Visibility.Collapsed;
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
                if (TxtDocStats != null) TxtDocStats.Text = $"Format: {_activeResult.OriginalFormat} • {_activeResult.WordCount:N0} ta so'z • {_activeResult.CharCount:N0} ta belgi (AI Verified)";
            }
            if (PnlAiReviewModal != null) PnlAiReviewModal.Visibility = Visibility.Collapsed;
            if (TxtStatus != null) TxtStatus.Text = "Applied!";
            MessageBox.Show("Markdown hujjati AI tuzatishlari bilan yangilandi!", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnCopyMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (TxtMarkdownEditor != null && !string.IsNullOrEmpty(TxtMarkdownEditor.Text))
        {
            Clipboard.SetText(TxtMarkdownEditor.Text);
            if (TxtStatus != null) TxtStatus.Text = "Copied to Clipboard!";
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
            if (TxtStatus != null) TxtStatus.Text = $"Saved: {dlg.FileName}";
            MessageBox.Show($"Markdown file saved!\n{dlg.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}