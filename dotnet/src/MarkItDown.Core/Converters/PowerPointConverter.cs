using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

namespace MarkItDown.Core.Converters;

public class PowerPointConverter
{
    public Task<string> ConvertAsync(byte[] pptxBytes, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Taqdimot Slaydlari");
        sb.AppendLine();

        using var stream = new MemoryStream(pptxBytes);
        using var presentationDoc = PresentationDocument.Open(stream, false);

        var presentationPart = presentationDoc.PresentationPart;
        if (presentationPart == null || presentationPart.Presentation.SlideIdList == null)
        {
            return Task.FromResult(sb.ToString().Trim());
        }

        var slideIdList = presentationPart.Presentation.SlideIdList.ChildElements;
        var slideIndex = 1;

        foreach (var slideIdElement in slideIdList)
        {
            ct.ThrowIfCancellationRequested();
            if (slideIdElement is not SlideId slideId || slideId.RelationshipId == null) continue;

            var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId.Value!);
            var slideTexts = ExtractSlideTexts(slidePart);

            if (slideTexts.Count > 0)
            {
                var title = slideTexts[0];
                var bodyTexts = slideTexts.Skip(1).ToList();

                sb.AppendLine($"## Slayd {slideIndex}: {title}");
                sb.AppendLine();

                foreach (var bullet in bodyTexts)
                {
                    sb.AppendLine($"- {bullet}");
                }

                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"## Slayd {slideIndex}");
                sb.AppendLine();
                sb.AppendLine("*(Slaydda o'qiladigan matn topilmadi)*");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            slideIndex++;
        }

        return Task.FromResult(sb.ToString().Trim());
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
