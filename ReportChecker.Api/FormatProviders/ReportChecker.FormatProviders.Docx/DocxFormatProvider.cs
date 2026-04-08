using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.FormatProviders.Docx;

public class DocxFormatProvider(IConfiguration configuration) : IFormatProvider
{
    public string Key => "Docx";

    private string ChapterSeparator { get; } = configuration["Reports.ChapterSeparator"] ?? "//";

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(IFileArchive archive)
    {
        var chapters = new List<Chapter>();
        var currentText = new StringBuilder();
        var path = new List<string> { "" };

        await using var stream = await archive.ReadAsync();
        using var document = WordprocessingDocument.Open(stream ?? throw new Exception(), false);
        var headingStyles = GuessHeadingStyles(document);
        foreach (var paragraph in document.MainDocumentPart?.Document?.Body?.Elements<Paragraph>() ?? [])
        {
            var style = GetParagraphStyle(paragraph);
            var level = headingStyles.GetValueOrDefault(style ?? "", 10);
            Console.WriteLine(level);
            // Проверяем стиль параграфа
            if (style != null && level <= 3)
            {
                // Сохраняем предыдущую главу
                if (currentText.Length > 0)
                {
                    chapters.Add(new Chapter
                    {
                        Name = string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e))),
                        Content = currentText.ToString(),
                    });
                    currentText.Clear();
                }

                // Создаем новую главу
                while (level < path.Count)
                    path.RemoveAt(path.Count - 1);
                while (level > path.Count)
                    path.Add("");
                path.Add(paragraph.InnerText);
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
                Name = string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e))),
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

    private static Dictionary<string, int> GuessHeadingStyles(WordprocessingDocument document)
    {
        var result = new Dictionary<string, int>();

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
                    if (level <= 2 && style.StyleId?.Value != null)
                        result[style.StyleId.Value] = level;
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