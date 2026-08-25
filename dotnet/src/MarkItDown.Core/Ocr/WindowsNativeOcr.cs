using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace MarkItDown.Core.Ocr;

public static class WindowsNativeOcr
{
    public static bool IsSupported => OcrEngine.AvailableRecognizerLanguages.Count > 0;

    public static async Task<string> RecognizeTextAsync(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0) return string.Empty;

        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageBytes.AsBuffer());
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);

            // Upscale image if small to ensure maximum OCR accuracy (Windows OCR works best at 1600px+)
            var originalWidth = decoder.PixelWidth;
            var originalHeight = decoder.PixelHeight;

            var transform = new BitmapTransform();
            if (originalWidth < 1400 || originalHeight < 1400)
            {
                var scale = Math.Max(2.0, 1800.0 / Math.Max(originalWidth, originalHeight));
                transform.ScaledWidth = (uint)(originalWidth * scale);
                transform.ScaledHeight = (uint)(originalHeight * scale);
                transform.InterpolationMode = BitmapInterpolationMode.Fant;
            }

            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            // Find best engines: Russian (for Cyrillic) and English (for Latin)
            var available = OcrEngine.AvailableRecognizerLanguages;
            var ruLang = available.FirstOrDefault(l => l.LanguageTag.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
                                                    || l.LanguageTag.StartsWith("uz-Cyrl", StringComparison.OrdinalIgnoreCase));
            var enLang = available.FirstOrDefault(l => l.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                                                    || l.LanguageTag.StartsWith("uz-Latn", StringComparison.OrdinalIgnoreCase));

            var enginesToTry = new List<OcrEngine>();
            if (ruLang != null)
            {
                var engRu = OcrEngine.TryCreateFromLanguage(ruLang);
                if (engRu != null) enginesToTry.Add(engRu);
            }
            if (enLang != null)
            {
                var engEn = OcrEngine.TryCreateFromLanguage(enLang);
                if (engEn != null) enginesToTry.Add(engEn);
            }
            if (enginesToTry.Count == 0 && available.Count > 0)
            {
                var defaultEngine = OcrEngine.TryCreateFromLanguage(available[0]);
                if (defaultEngine != null) enginesToTry.Add(defaultEngine);
            }

            if (enginesToTry.Count == 0) return string.Empty;

            var bestLines = new List<string>();
            var bestScore = -1;

            foreach (var engine in enginesToTry)
            {
                var result = await engine.RecognizeAsync(softwareBitmap);
                if (result != null && result.Lines.Count > 0)
                {
                    var lines = new List<(double Y, double X, string Text)>();
                    foreach (var line in result.Lines)
                    {
                        var text = line.Text.Trim();
                        if (string.IsNullOrEmpty(text)) continue;

                        // Calculate vertical position from words
                        var y = line.Words.Count > 0 ? line.Words[0].BoundingRect.Y : 0;
                        var x = line.Words.Count > 0 ? line.Words[0].BoundingRect.X : 0;
                        lines.Add((y, x, text));
                    }

                    // Sort lines in top-to-bottom reading order
                    var sortedLines = lines.OrderBy(l => l.Y).Select(l => l.Text).ToList();
                    var rawText = string.Join("\n", sortedLines);
                    var cyrillicCharCount = rawText.Count(c => (c >= 'А' && c <= 'я') || c == 'Ё' || c == 'ё');
                    var score = sortedLines.Count * 15 + cyrillicCharCount * 2;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestLines = sortedLines;
                    }
                }
            }

            if (bestLines.Count == 0) return string.Empty;

            // Intelligent Post-Processing Clean-Up & Uzbek/Cyrillic Case Normalizer
            return CleanAndFormatOcrLines(bestLines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WindowsNativeOcr] Xatolik: {ex.Message}");
            return string.Empty;
        }
    }

    public static string CleanAndFormatOcrLines(List<string> rawLines)
    {
        if (rawLines == null || rawLines.Count == 0) return string.Empty;

        var cleanedLines = new List<string>();

        foreach (var originalLine in rawLines)
        {
            var line = originalLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // Strip stray borders/ornament artifact lines like "-----" or "~~~~~" or lone quotes
            if (Regex.IsMatch(line, @"^[—\-=_~`|\.\,\:\;\*\s\<\>\(\)\{\}\[\]\^\@\#\$\%\&]+$"))
            {
                continue;
            }

            // Remove leading/trailing symbols except numbers and letters
            line = Regex.Replace(line, @"^[|~_`•\—\s]+|[|~_`•\—\s]+$", "");

            // Single characters: allow numbers (1, 2, 3...) or Roman numerals (I, V, X) or single valid letters
            if (line.Length == 1 && !char.IsLetterOrDigit(line[0]))
            {
                continue;
            }

            // Apply case-preserving dictionary fixes for Uzbek & common OCR misrecognitions
            line = FixUzbekWordsWithCasePreservation(line);

            // Harmonize line casing: If line is predominantly UPPERCASE (e.g. "АБДУЛЛА ҚОДИРИЙ", "ХАЛҚ МЕРОСИ"), make entire line UPPERCASE
            line = HarmonizeLineCase(line);

            // Clean spaces around punctuation
            line = Regex.Replace(line, @"\s+([,.:;?!])", "$1");
            line = Regex.Replace(line, @"([,.:;?!])([^\s0-9])", "$1 $2");
            line = Regex.Replace(line, @"\s{2,}", " ");

            cleanedLines.Add(line.Trim());
        }

        return string.Join("\n", cleanedLines).Trim();
    }

    private static string FixUzbekWordsWithCasePreservation(string line)
    {
        // Dictionary of words: Pattern -> Correct Uzbek spelling
        var replacements = new (string Pattern, string Target)[]
        {
            // Title words
            (@"\bконуила?ри\b", "қонунлари"),
            (@"\bконунлари\b", "қонунлари"),
            (@"\bконуни\b", "қонуни"),
            (@"\bсайланма\b", "сайланма"),
            (@"\bжилдлик\b", "жилдлик"),
            (@"\bжилд\b", "жилд"),

            // Names & Locations
            (@"\bабу\b", "абу"),
            (@"\bали\b", "али"),
            (@"\bибн\b", "ибн"),
            (@"\bсино\b", "сино"),
            (@"\bтоwкеht\b", "тошкент"),
            (@"\btowkeht\b", "тошкент"),
            (@"\bтошкент\b", "тошкент"),
            (@"\bабдулла\b", "абдулла"),
            (@"\bкодирий\b", "қодирий"),
            (@"\bкодирий,\b", "қодирий"),
            (@"\bномиддги\b", "номидаги"),
            (@"\bномидаги\b", "номидаги"),
            (@"\bхалк\b", "халқ"),
            (@"\bхалк,\b", "халқ"),
            (@"\bмероси\b", "мероси"),
            (@"\bнашриёти\b", "нашриёти"),

            // State & Academic terms
            (@"\bузбекистон\b", "ўзбекистон"),
            (@"\bкулёзма\b", "қўлёзма"),
            (@"\bкисм\b", "қисм"),
            (@"\bкулланма\b", "қўлланма"),
            (@"\bжумхурияти\b", "жумҳурияти"),
            (@"\bкитоби\b", "китоби")
        };

        foreach (var (pattern, target) in replacements)
        {
            line = Regex.Replace(line, pattern, match =>
            {
                var orig = match.Value.TrimEnd(',', '.');
                var hasComma = match.Value.EndsWith(",");
                var hasPeriod = match.Value.EndsWith(".");

                var rep = MatchWordCase(orig, target);
                if (hasComma) rep += ",";
                if (hasPeriod) rep += ".";
                return rep;
            }, RegexOptions.IgnoreCase);
        }

        return line;
    }

    private static string MatchWordCase(string original, string target)
    {
        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(target)) return target;

        var letters = original.Where(char.IsLetter).ToList();
        if (letters.Count == 0) return target;

        // If original is ALL UPPERCASE (e.g. "КОДИРИЙ", "ХАЛК", "УЧ", "ЖИЛД") -> "ҚОДИРИЙ", "ХАЛҚ"
        if (letters.All(char.IsUpper))
        {
            return target.ToUpperInvariant();
        }

        // If original is TitleCase (e.g. "Кодирий", "Халк") -> "Қодирий", "Халқ"
        if (char.IsUpper(letters[0]) && letters.Skip(1).All(char.IsLower))
        {
            return char.ToUpper(target[0]) + (target.Length > 1 ? target.Substring(1).ToLowerInvariant() : "");
        }

        // If original is all lowercase -> all lowercase
        if (letters.All(char.IsLower))
        {
            return target.ToLowerInvariant();
        }

        // Mixed case OCR glitch (e.g. "конуилаРи") -> TitleCase if first is Upper, else lowercase
        if (char.IsUpper(original[0]))
        {
            return char.ToUpper(target[0]) + (target.Length > 1 ? target.Substring(1).ToLowerInvariant() : "");
        }

        return target.ToLowerInvariant();
    }

    private static string HarmonizeLineCase(string line)
    {
        var words = Regex.Matches(line, @"\p{L}+").Select(m => m.Value).ToList();
        if (words.Count <= 1) return line;

        var upperWords = words.Count(w => w.All(char.IsUpper) && w.Length > 1);

        // If 60%+ of the words in the line are ALL UPPERCASE (e.g. "АБДУЛЛА қодирий", "халқ МЕРОСИ", "уч ЖИЛДЛИК САЙЛАНМА")
        // Make the entire line UPPERCASE for consistency!
        if ((double)upperWords / words.Count >= 0.5)
        {
            return line.ToUpperInvariant();
        }

        return line;
    }
}
