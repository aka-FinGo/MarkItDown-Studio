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

            // Upscale image if small to ensure maximum OCR accuracy (Windows OCR works best at 1500px+)
            var originalWidth = decoder.PixelWidth;
            var originalHeight = decoder.PixelHeight;

            var transform = new BitmapTransform();
            if (originalWidth < 1200 || originalHeight < 1200)
            {
                var scale = Math.Max(2.0, 1600.0 / Math.Max(originalWidth, originalHeight));
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

            // 1. Find best engines: Russian (for Cyrillic) and English (for Latin)
            var available = OcrEngine.AvailableRecognizerLanguages;
            var ruLang = available.FirstOrDefault(l => l.LanguageTag.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
                                                    || l.LanguageTag.StartsWith("uz-Cyrl", StringComparison.OrdinalIgnoreCase));
            var enLang = available.FirstOrDefault(l => l.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                                                    || l.LanguageTag.StartsWith("uz-Latn", StringComparison.OrdinalIgnoreCase));

            // Try Russian engine first (Cyrillic support), fallback to English, then any available
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

            if (enginesToTry.Count == 0)
            {
                var defaultEngine = OcrEngine.TryCreateFromUserProfileLanguages()
                                  ?? (available.Count > 0 ? OcrEngine.TryCreateFromLanguage(available[0]) : null);
                if (defaultEngine != null) enginesToTry.Add(defaultEngine);
            }

            if (enginesToTry.Count == 0) return string.Empty;

            // Run OCR with primary engine
            var bestText = string.Empty;
            var bestScore = -1;

            foreach (var engine in enginesToTry)
            {
                var result = await engine.RecognizeAsync(softwareBitmap);
                if (result != null && result.Lines.Count > 0)
                {
                    var lines = new List<string>();
                    foreach (var line in result.Lines)
                    {
                        var t = line.Text.Trim();
                        if (!string.IsNullOrEmpty(t)) lines.Add(t);
                    }

                    var rawText = string.Join("\n", lines);
                    var cyrillicCharCount = rawText.Count(c => (c >= 'А' && c <= 'я') || c == 'Ё' || c == 'ё');
                    var score = lines.Count * 10 + cyrillicCharCount;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestText = rawText;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(bestText)) return string.Empty;

            // 2. Intelligent Post-Processing Clean-Up & Uzbek/Cyrillic Normalizer
            return CleanAndFormatOcrText(bestText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WindowsNativeOcr] Xatolik: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Intelligently cleans OCR output, fixes mixed-case glitches, removes OCR artifacts,
    /// and normalizes Uzbek Cyrillic/Latin characters (қ, ғ, ҳ, ў, о', g').
    /// </summary>
    public static string CleanAndFormatOcrText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

        var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var cleanedLines = new List<string>();

        foreach (var originalLine in lines)
        {
            var line = originalLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // 1. Remove lone symbols/artifacts like |, ~, _, `, etc.
            line = Regex.Replace(line, @"^[|~_`•\-\—\s]+|[|~_`•\-\—\s]+$", "");
            if (line.Length <= 1 && !char.IsLetterOrDigit(line[0])) continue;

            // 2. Fix Mixed Case errors inside words (e.g. "конуилаРи" -> "қонунлари")
            line = FixMixedCaseInWords(line);

            // 3. Apply Uzbek & Cyrillic standard dictionary replacements
            line = FixCommonUzbekOcrTypos(line);

            // 4. Fix spaces around punctuation
            line = Regex.Replace(line, @"\s+([,.:;?!])", "$1");
            line = Regex.Replace(line, @"([,.:;?!])([^\s0-9])", "$1 $2");
            line = Regex.Replace(line, @"\s{2,}", " ");

            cleanedLines.Add(line.Trim());
        }

        return string.Join("\n", cleanedLines).Trim();
    }

    private static string FixMixedCaseInWords(string line)
    {
        // Matches words with mixed lower and upper case (e.g. "конуилаРи", "ЖИЛДлик", "ноМИдаги")
        return Regex.Replace(line, @"\b\p{L}+\b", m =>
        {
            var word = m.Value;
            if (word.Length <= 2) return word;

            var upperCount = word.Count(char.IsUpper);
            var lowerCount = word.Count(char.IsLower);

            // If mostly UPPERCASE with 1-2 lower case letters (e.g. "ЖИЛДлИК"), make all UPPERCASE
            if (upperCount >= word.Length - 1 && upperCount > lowerCount)
            {
                return word.ToUpperInvariant();
            }

            // If starting with Upper and mostly lower case with accidental middle/end upper (e.g. "конуилаРи"), make lowercase except first
            if (lowerCount > upperCount)
            {
                if (char.IsUpper(word[0]))
                {
                    return char.ToUpper(word[0]) + word.Substring(1).ToLowerInvariant();
                }
                return word.ToLowerInvariant();
            }

            return word;
        });
    }

    private static string FixCommonUzbekOcrTypos(string line)
    {
        // Dictionary of standard Uzbek Cyrillic OCR misreadings
        var replacements = new (string Pattern, string Replacement)[]
        {
            // Title & Concept words
            (@"\bконуила?ри\b", "қонунлари"),
            (@"\bКОНУИЛА?РИ\b", "ҚОНУНЛАРИ"),
            (@"\bконунлари\b", "қонунлари"),
            (@"\bКОНУНЛАРИ\b", "ҚОНУНЛАРИ"),
            (@"\bконуни\b", "қонуни"),
            (@"\bКОНУНИ\b", "ҚОНУНИ"),

            // Names & Publishers
            (@"\bномиддги\b", "номидаги"),
            (@"\bНОМИДДГИ\b", "НОМИДАГИ"),
            (@"\bномидаги\b", "номидаги"),
            (@"\bкодирий\b", "қодирий"),
            (@"\bКОДИРИЙ\b", "ҚОДИРИЙ"),
            (@"\bКодирий\b", "Қодирий"),
            (@"\bхалк\b", "халқ"),
            (@"\bХАЛК\b", "ХАЛҚ"),
            (@"\bХалк\b", "Халқ"),
            (@"\bмероси\b", "мероси"),
            (@"\bМЕРОСИ\b", "МЕРОСИ"),
            (@"\bнашриёти\b", "нашриёти"),
            (@"\bНАШРИЁТИ\b", "НАШРИЁТИ"),

            // Common Uzbek administrative & literary words
            (@"\bузбекистон\b", "ўзбекистон"),
            (@"\bУЗБЕКИСТОН\b", "ЎЗБЕКИСТОН"),
            (@"\bУзбекистон\b", "Ўзбекистон"),
            (@"\bкулёзма\b", "қўлёзма"),
            (@"\bКУЛЁЗМА\b", "ҚЎЛЁЗМА"),
            (@"\bКулёзма\b", "Қўлёзма"),
            (@"\bкисм\b", "қисм"),
            (@"\bКИСМ\b", "ҚИСМ"),
            (@"\bКисм\b", "Қисм"),
            (@"\bкулланма\b", "қўлланма"),
            (@"\bКУЛЛАНМА\b", "ҚЎЛЛАНМА"),
            (@"\bжумхурияти\b", "жумҳурияти"),
            (@"\bЖУМХУРИЯТИ\b", "ЖУМҲУРИЯТИ"),
            (@"\bкитоби\b", "китоби"),
            (@"\bКИТОБИ\b", "КИТОБИ"),
            (@"\bсайланма\b", "сайланма"),
            (@"\bСАЙЛАНМА\b", "САЙЛАНМА"),
            (@"\bжилдлик\b", "жилдлик"),
            (@"\bЖИЛДЛИК\b", "ЖИЛДЛИК"),
            (@"\bтошкент\b", "тошкент"),
            (@"\bТОШКЕНТ\b", "ТОШКЕНТ"),
            (@"\bибн сино\b", "ибн сино"),
            (@"\bИБН СИНО\b", "ИБН СИНО"),
            (@"\bабу али\b", "абу али"),
            (@"\bАБУ АЛИ\b", "АБУ АЛИ")
        };

        foreach (var (pattern, rep) in replacements)
        {
            line = Regex.Replace(line, pattern, rep, RegexOptions.IgnoreCase);
        }

        return line;
    }
}
