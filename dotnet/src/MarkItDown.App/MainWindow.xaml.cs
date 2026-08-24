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
    public ObservableCollection<ConversionResult> ConvertedItems { get; } = new();
    private ConversionResult? _activeResult;
    private string? _customPrompt;

    public MainWindow()
    {
        InitializeComponent();
        _engine = new MarkItDownEngine();
        LstConvertedItems.ItemsSource = ConvertedItems;

        PopulateModelNames(AiProvider.GoogleGemini);
        LoadDefaultSample();
    }

    private void LoadDefaultSample()
    {
        var welcomeMd = @"# MarkItDown Studio .NET ga Xush Kelibsiz! 🚀

Ushbu dastur **Microsoft MarkItDown** va **MarkItDown Studio** loyihalarini C# (.NET) da mukammal birlashtirgan universal konverterdir.

---

### 🌟 Asosiy Imkoniyatlar:
- **PDF (.pdf):** Matnli va skanerlangan (OCR) PDF fayllarni to'liq o'qish (bo'sh sahifa chiqmaydi!).
- **Word (.docx):** Sarlavhalar, jadvallar va ro'yxatlarni toza Markdown ga o'girish.
- **Excel (.xlsx, .xls, .csv):** Barcha sahifalarni toza GFM Markdown jadvaliga aylantirish.
- **PowerPoint (.pptx):** Slaydlar va taqdimot matnlarini tartibli ajratish.
- **Rasm (OCR) & Audio:** Google Gemini, OpenAI, Claude, DeepSeek, Ollama orqali matnga o'girish.
- **ZIP Arxivlar:** Arxiv ichidagi barcha fayllarni birvarakayiga konvertatsiya qilish.
- **Web Havolalar (URL):** Web maqolalarni to'g'ridan-to'g'ri toza Markdown ga aylantirish.

---

### 💡 Boshlash uchun:
Chap tarafdagi maydonga faylingizni tashlang yoki **'Fayl Tanlash'** tugmasini bosing!
";
        TxtMarkdownEditor.Text = welcomeMd;
    }

    // Title Bar Drag & Window Controls
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

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

    private AiProviderConfig GetCurrentAiConfig()
    {
        var provider = AiProvider.GoogleGemini;
        if (CmbAiProvider.SelectedItem is ComboBoxItem item && Enum.TryParse<AiProvider>(item.Tag?.ToString(), out var p))
        {
            provider = p;
        }

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
            IncludeFrontmatter = ChkFrontmatter.IsChecked == true,
            CustomPrompt = _customPrompt,
            AutoOcrScannedPdf = true
        };
    }

    // Drag and Drop Handlers
    private void DropArea_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(129, 140, 248));
            DropArea.Background = new SolidColorBrush(Color.FromArgb(200, 49, 46, 129));
        }
    }

    private void DropArea_DragLeave(object sender, DragEventArgs e)
    {
        DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
        DropArea.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
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

    // Select File Button
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

    // Process Files
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

    // Convert URL
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
            Filter = "Markdown Hujjati (*.md)|*.md|Matn Hujjati (*.txt)|*.txt|Barcha Fayllar (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, TxtMarkdownEditor.Text, System.Text.Encoding.UTF8);
            TxtStatus.Text = $"Fayl saqlandi: {dlg.FileName}";
            MessageBox.Show($"Markdown fayli muvaffaqiyatli saqlandi!\n{dlg.FileName}", "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}