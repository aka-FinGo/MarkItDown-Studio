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

            // Upscale image if needed to ensure maximum OCR sharpness
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
                    var rawItems = new List<(double Y, double X, double Width, string Text)>();
                    foreach (var line in result.Lines)
                    {
                        var text = line.Text.Trim();
                        if (string.IsNullOrEmpty(text)) continue;

                        var y = line.Words.Count > 0 ? line.Words[0].BoundingRect.Y : 0;
                        var x = line.Words.Count > 0 ? line.Words[0].BoundingRect.X : 0;
                        var w = line.Words.Count > 0 ? line.Words.Last().BoundingRect.X + line.Words.Last().BoundingRect.Width - x : 0;
                        rawItems.Add((y, x, w, text));
                    }

                    // Smart 2-Column Spatial Reading Order Detection
                    var sortedLines = ProcessSpatialReadingOrder(rawItems, softwareBitmap.PixelWidth);

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

            // Intelligent Post-Processing Clean-Up & Uzbek Cyrillic/Latin Vocalic Restorer
            return CleanAndFormatOcrLines(bestLines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WindowsNativeOcr] Xatolik: {ex.Message}");
            return string.Empty;
        }
    }

    private static List<string> ProcessSpatialReadingOrder(List<(double Y, double X, double Width, string Text)> items, int pageWidth)
    {
        if (items.Count <= 2) return items.OrderBy(i => i.Y).Select(i => i.Text).ToList();

        // Check if there is a distinct 2-column layout (significant lines on left and right)
        var midX = pageWidth / 2.0;
        var leftLines = items.Where(i => i.X + i.Width * 0.5 < midX).ToList();
        var rightLines = items.Where(i => i.X >= midX * 0.75).ToList();

        // If both left and right columns have at least 25% of total items, sort left column first, then right column!
        if (leftLines.Count >= items.Count * 0.25 && rightLines.Count >= items.Count * 0.25)
        {
            var headerLines = items.Where(i => i.Y < items.Min(x => x.Y) + 60 && i.Width > pageWidth * 0.6).OrderBy(i => i.Y).ToList();
            var leftSorted = leftLines.Except(headerLines).OrderBy(i => i.Y).ToList();
            var rightSorted = rightLines.Except(headerLines).OrderBy(i => i.Y).ToList();

            var combined = new List<string>();
            foreach (var h in headerLines) combined.Add(h.Text);
            foreach (var l in leftSorted) combined.Add(l.Text);
            foreach (var r in rightSorted) combined.Add(r.Text);

            return combined;
        }

        // Standard single column top-to-bottom sorting
        return items.OrderBy(i => i.Y).Select(i => i.Text).ToList();
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

            // Harmonize line casing: If line is predominantly UPPERCASE, make entire line UPPERCASE
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
        // 1. Vocalic Glitch Normalization (e.g. "бУлса" -> "бўлса", "кУз" -> "кўз", "Упка" -> "ўпка")
        line = Regex.Replace(line, @"\bбУл([а-я]+)?\b", m => MatchWordCase(m.Value, "бўл" + m.Groups[1].Value), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bкУз([а-я]+)?\b", m => MatchWordCase(m.Value, "кўз" + m.Groups[1].Value), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bУпк([а-я]+)?\b", m => MatchWordCase(m.Value, "ўпк" + m.Groups[1].Value), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bкУр([а-я]+)?\b", m => MatchWordCase(m.Value, "кўр" + m.Groups[1].Value), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bУрг([а-я]+)?\b", m => MatchWordCase(m.Value, "ўрг" + m.Groups[1].Value), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bУт([а-я]+)?\b", m => MatchWordCase(m.Value, "ўт" + m.Groups[1].Value), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bшабкУрлик\b", m => MatchWordCase(m.Value, "шабкўрлик"), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bкУкрак\b", m => MatchWordCase(m.Value, "кўкрак"), RegexOptions.IgnoreCase);

        // 2. Tuturq Belgisi (Hard Sign 'ъ') in Uzbek Cyrillic words
        line = Regex.Replace(line, @"\bса[ь']дулла\b", m => MatchWordCase(m.Value, "саъдулла"), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bма[ь']сул\b", m => MatchWordCase(m.Value, "масъул"), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bта[ь']лим\b", m => MatchWordCase(m.Value, "таълим"), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bэ[ь']лон\b", m => MatchWordCase(m.Value, "эълон"), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bма[ь']руза\b", m => MatchWordCase(m.Value, "маъруза"), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bма[ь']лумот\b", m => MatchWordCase(m.Value, "маълумот"), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bмў[ь']табар\b", m => MatchWordCase(m.Value, "мўътабар"), RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"\bжуз[ь']ий\b", m => MatchWordCase(m.Value, "жузъий"), RegexOptions.IgnoreCase);

        // 3. High-Precision Vocabulary Dictionary for Publishing & Classical Uzbek Literature
        var replacements = new (string Pattern, string Target)[]
        {
            // Publishing & Colophon Information
            (@"\bилмий[- ]*с[ио]ммабо[пр]\b", "илмий-оммабоп"),
            (@"\bилмий[- ]*оммабоп\b", "илмий-оммабоп"),
            (@"\bмуса[,\s]*[ҳх]лила\b", "мусаҳҳиҳа"),
            (@"\bмусаҳҳиҳа\b", "мусаҳҳиҳа"),
            (@"\bмуцаххиха\b", "мусаҳҳиҳа"),
            (@"\bму[ца][а-я]*ррир\b", "муҳаррир"),
            (@"\bмухаррир\b", "муҳаррир"),
            (@"\bмуҳаррир\b", "муҳаррир"),
            (@"\bбосишк[*\sа-я]*а\b", "босишга"),
            (@"\bбосишга\b", "босишга"),
            (@"\bрухсат этилди\b", "рухсат этилди"),
            (@"\bбосмахона\b", "босмахона"),
            (@"\bкорози\b", "қоғози"),
            (@"\bқорози\b", "қоғози"),
            (@"\bкоғози\b", "қоғози"),
            (@"\bқоғози\b", "қоғози"),
            (@"\bюкори\b", "юқори"),
            (@"\bюқори\b", "юқори"),
            (@"\bта[б6]о[ғгf][ки]?\b", "табоғи"),
            (@"\bтабоғи\b", "табоғи"),
            (@"\bтабори\b", "табоғи"),
            (@"\bкизи\b", "қизи"),
            (@"\bқизи\b", "қизи"),
            (@"\bшартно[ма]*\b", "шартнома"),
            (@"\bадабий гарнитура\b", "адабий гарнитура"),
            (@"\bтеришга берилди\b", "теришга берилди"),
            (@"\bбичи\s*ми\b", "бичими"),
            (@"\bусу\s*лида\b", "усулида"),
            (@"\bкелишилган нархда\b", "келишилган нархда"),

            // Books, authors and publications
            (@"\bконуила?ри\b", "қонунлари"),
            (@"\bконунлари\b", "қонунлари"),
            (@"\bконуни\b", "қонуни"),
            (@"\bсайланма\b", "сайланма"),
            (@"\bжилдлик\b", "жилдлик"),
            (@"\bжилд\b", "жилд"),
            (@"\bабу\b", "абу"),
            (@"\bали\b", "али"),
            (@"\bибн\b", "ибн"),
            (@"\bсино\b", "сино"),
            (@"\bтоwкеht\b", "тошкент"),
            (@"\btowkeht\b", "тошкент"),
            (@"\bтошкент\b", "тошкент"),
            (@"\bабдулла\b", "абдулла"),
            (@"\bкодирий\b", "қодирий"),
            (@"\bномиддги\b", "номидаги"),
            (@"\bномидаги\b", "номидаги"),
            (@"\bхалк\b", "халқ"),
            (@"\bмероси\b", "мероси"),
            (@"\bнашриёти\b", "нашриёти"),
            (@"\bмунда\s*рижа\b", "МУНДАРИЖА"),

            // Medical & Scientific Terms (Ibn Sino Canon)
            (@"\bлилинган\b", "қилинган"),
            (@"\bкилинган\b", "қилинган"),
            (@"\bлилади\b", "қилади"),
            (@"\bкяилади\b", "қилади"),
            (@"\bкилади\b", "қилади"),
            (@"\bлувват(и)?\b", "қувват$1"),
            (@"\bтарёк\b", "тарёқ"),
            (@"\bтарёл\b", "тарёқ"),
            (@"\bоррик\b", "оғриқ"),
            (@"\bорриги\b", "оғриғи"),
            (@"\bорриклар\b", "оғриқлар"),
            (@"\bуруги\b", "уруғи"),
            (@"\bларорат\b", "ҳарорат"),
            (@"\bмелригиёл\b", "меҳригиёҳ"),
            (@"\bажволини\b", "аҳволини"),
            (@"\bлаттик\b", "қаттиқ"),
            (@"\bиссиллик\b", "иссиқлик"),
            (@"\bиссик\b", "иссиқ"),
            (@"\bлукна\b", "ҳуқна"),
            (@"\bхукна\b", "ҳуқна"),
            (@"\bлуйиладиган\b", "қуйиладиган"),
            (@"\bчорда\b", "чоғда"),
            (@"\bсарил\b", "сариқ"),
            (@"\bкалтирол\b", "қалтироқ"),
            (@"\bтуррисида\b", "тўғрисида"),
            (@"\bтугрисида\b", "тўғрисида"),
            (@"\bяллигланиши\b", "яллиғланиши"),
            (@"\bяллирланиши\b", "яллиғланиши"),
            (@"\bсоглигини\b", "соғлиғини"),
            (@"\bсорлигини\b", "соғлиғини"),
            (@"\bаъзолар(да)?\b", "аъзолар$1"),
            (@"\bмигрень\b", "мигрень"),
            (@"\bзотурриа\b", "зотуррия"),
            (@"\bзотуррия\b", "зотуррия"),
            (@"\bларсиллаш\b", "ҳарсиллаш"),
            (@"\bхарсиллаш\b", "ҳарсиллаш"),
            (@"\bа\s*с\s*тм\s*а\b", "астма"),
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

        if (letters.All(char.IsUpper))
        {
            return target.ToUpperInvariant();
        }

        if (char.IsUpper(letters[0]) && letters.Skip(1).All(char.IsLower))
        {
            return char.ToUpper(target[0]) + (target.Length > 1 ? target.Substring(1).ToLowerInvariant() : "");
        }

        if (letters.All(char.IsLower))
        {
            return target.ToLowerInvariant();
        }

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

        if ((double)upperWords / words.Count >= 0.5)
        {
            return line.ToUpperInvariant();
        }

        return line;
    }
}
