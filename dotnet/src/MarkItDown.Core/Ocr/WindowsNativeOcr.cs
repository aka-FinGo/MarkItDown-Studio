using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            var engine = OcrEngine.TryCreateFromUserProfileLanguages()
                      ?? (OcrEngine.AvailableRecognizerLanguages.Count > 0
                          ? OcrEngine.TryCreateFromLanguage(OcrEngine.AvailableRecognizerLanguages[0])
                          : null);

            if (engine == null)
            {
                return string.Empty;
            }

            var ocrResult = await engine.RecognizeAsync(softwareBitmap);

            if (ocrResult == null || ocrResult.Lines.Count == 0)
            {
                return ocrResult?.Text ?? string.Empty;
            }

            // Organize lines cleanly
            var sb = new StringBuilder();
            foreach (var line in ocrResult.Lines)
            {
                var lineText = line.Text.Trim();
                if (!string.IsNullOrEmpty(lineText))
                {
                    sb.AppendLine(lineText);
                }
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WindowsNativeOcr] Xatolik: {ex.Message}");
            return string.Empty;
        }
    }
}
