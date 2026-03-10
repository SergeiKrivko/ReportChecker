using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.FormatProviders.Docx;

public class DocxFormatProvider : IFormatProvider
{
    public string Key => "Docx";

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(IFileArchive archive)
    {
        var chapters = new List<Chapter>();
        string? currentChapter = null;
        var currentText = new StringBuilder();

        await using var stream = await archive.OpenAsync();
        using var document = WordprocessingDocument.Open(stream ?? throw new Exception(), false);
        var headingStyles = GuessHeadingStyles(document);
        foreach (var paragraph in document.MainDocumentPart?.Document?.Body?.Elements<Paragraph>() ?? [])
        {
            var style = GetParagraphStyle(paragraph);
            // Проверяем стиль параграфа
            if (style != null && headingStyles.Contains(style))
            {
                // Сохраняем предыдущую главу
                if (currentChapter != null && currentText.Length > 0)
                {
                    chapters.Add(new Chapter
                    {
                        Name = currentChapter,
                        Content = currentText.ToString(),
                    });
                    currentText.Clear();
                }

                // Создаем новую главу
                currentChapter = paragraph.InnerText;
            }
            else
            {
                // Добавляем текст в текущую главу
                currentText.AppendLine(paragraph.InnerText);
            }
        }

        // Добавляем последнюю главу
        if (currentText.Length > 0)
        {
            chapters.Add(new Chapter
            {
                Name = currentChapter ?? "<Root>",
                Content = currentText.ToString(),
            });
        }

        return chapters;
    }

    private static string? GetParagraphStyle(Paragraph paragraph)
    {
        var styleProp = paragraph.ParagraphProperties?.ParagraphStyleId;
        if (styleProp != null && styleProp.Val != null)
        {
            return styleProp.Val.Value;
        }

        return null;
    }

    private static HashSet<string> GuessHeadingStyles(WordprocessingDocument document)
    {
        var result = new HashSet<string>();

        var stylesPart = document.MainDocumentPart?.StyleDefinitionsPart;
        if (stylesPart != null)
        {
            foreach (var style in stylesPart.Styles?.Elements<Style>() ?? [])
            {
                // Проверяем, есть ли у стиля уровень структуры в определении
                var outlineLvl = style.StyleParagraphProperties?.OutlineLevel;
                if (outlineLvl?.Val != null)
                {
                    int level = outlineLvl.Val.Value;
                    Console.WriteLine(level);
                    if (level <= 2 && style.StyleId != null)
                        result.Add(style.StyleId.Value ?? "");
                }
            }
        }

        return result;
    }

    public Task<bool> TestSourceAsync(IFileArchive archive)
    {
        return Task.FromResult(archive.Name?.EndsWith(".docx") ?? false);
    }
}