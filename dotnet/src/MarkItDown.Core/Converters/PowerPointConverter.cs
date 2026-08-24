using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using MarkItDown.Core.Ai;
using MarkItDown.Core.Models;
using A = DocumentFormat.OpenXml.Drawing;

namespace MarkItDown.Core.Converters;

public class PowerPointConverter
{
    private readonly IUniversalAiClient _aiClient;

    public PowerPointConverter(IUniversalAiClient? aiClient = null)
    {
        _aiClient = aiClient ?? new UniversalAiClient();
    }

    public async Task<string> ConvertAsync(
        byte[] pptxBytes,
        string fileName,
        string? outputDirectory,
        ConversionOptions options,
        AiProviderConfig? aiConfig,
        CancellationToken ct = default)
    {
        var cleanDocName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_");
        var attachmentFolder = $"{cleanDocName}_attachments";
        var fullAttachmentPath = !string.IsNullOrEmpty(outputDirectory)
            ? Path.Combine(outputDirectory, attachmentFolder)
            : Path.Combine(Environment.CurrentDirectory, attachmentFolder);

        using var stream = new MemoryStream(pptxBytes);
        using var presentationDoc = PresentationDocument.Open(stream, false);

        var presentationPart = presentationDoc.PresentationPart;
        if (presentationPart == null || presentationPart.Presentation.SlideIdList == null)
        {
            return string.Empty;
        }

        var slideIdList = presentationPart.Presentation.SlideIdList.ChildElements;
        var slideCount = slideIdList.Count;

        var sb = new StringBuilder();
        sb.AppendLine($"# 📄 {Path.GetFileNameWithoutExtension(fileName)}");
        sb.AppendLine();
        sb.AppendLine($"> 📌 **Hujjat:** `{fileName}` | **Slaydlar:** {slideCount} ta | **Format:** PowerPoint (.pptx)");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        var hasApiKey = options.EnableAi && aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey);
        var slideIndex = 1;
        var imgIndex = 1;

        foreach (var slideIdElement in slideIdList)
        {
            ct.ThrowIfCancellationRequested();
            if (slideIdElement is not SlideId slideId || slideId.RelationshipId == null) continue;

            var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId.Value!);
            var slideTexts = ExtractSlideTexts(slidePart);

            var title = slideTexts.Count > 0 ? slideTexts[0] : $"Slayd {slideIndex}";
            var bodyTexts = slideTexts.Count > 0 ? slideTexts.Skip(1).ToList() : new List<string>();

            sb.AppendLine($"## Slayd {slideIndex}: {title}");
            sb.AppendLine();

            foreach (var bullet in bodyTexts)
            {
                sb.AppendLine($"- {bullet}");
            }

            // Extract images on this slide
            foreach (var imgPart in slidePart.ImageParts)
            {
                using var imgStream = imgPart.GetStream();
                using var ms = new MemoryStream();
                await imgStream.CopyToAsync(ms, ct);
                var imgData = ms.ToArray();

                if (imgData.Length > 100)
                {
                    try { Directory.CreateDirectory(fullAttachmentPath); } catch { }
                    var imgName = $"pptx_slide{slideIndex}_img{imgIndex}.png";
                    var relativeImgPath = $"{attachmentFolder}/{imgName}";
                    var savePath = Path.Combine(fullAttachmentPath, imgName);
                    try { await File.WriteAllBytesAsync(savePath, imgData, ct); } catch { }

                    sb.AppendLine($"![{imgName}]({relativeImgPath})");
                    if (hasApiKey && aiConfig != null)
                    {
                        try
                        {
                            var (ocrRes, _) = await _aiClient.ConvertWithAiAsync(imgData, "image/png", imgName, aiConfig, options.CustomPrompt, ct);
                            sb.AppendLine($"> 🤖 **[AI OCR / Tasvir Tahlili]** *(Ushbu qism `{aiConfig.Provider}` - `{aiConfig.ModelName}` modeli yordamida tayyorlandi, tekshirib ko'ring)*:\n>\n> " + ocrRes.Replace("\n", "\n> "));
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"> ⚠️ *(Ushbu rasm `{relativeImgPath}` manzilida saqlandi. OCR xatosi: {ex.Message})*");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"> ⚠️ *(Ushbu rasm `{relativeImgPath}` manzilida saqlandi. AI API kaliti ulanmagani sababli rasmdagi matn ajratib olinmadi)*");
                    }
                    sb.AppendLine();
                    imgIndex++;
                }
            }

            if (slideIndex < slideCount)
            {
                sb.AppendLine("---");
                sb.AppendLine();
            }

            slideIndex++;
        }

        return sb.ToString().Trim();
    }

    private static List<string> ExtractSlideTexts(SlidePart slidePart)
    {
        var result = new List<string>();
        var slide = slidePart.Slide;
        if (slide == null) return result;

        var textElements = slide.Descendants<A.Paragraph>();
        foreach (var paragraph in textElements)
        {
            var sb = new StringBuilder();
            foreach (var text in paragraph.Descendants<A.Text>())
            {
                sb.Append(text.Text);
            }

            var clean = sb.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(clean))
            {
                result.Add(clean);
            }
        }

        return result;
    }
}
