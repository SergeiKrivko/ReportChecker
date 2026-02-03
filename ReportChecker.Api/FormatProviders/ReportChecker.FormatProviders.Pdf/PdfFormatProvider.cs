using System.Text;
using ReportChecker.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Outline;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.FormatProviders.Pdf;

public class PdfFormatProvider : IFormatProvider
{
    public string Key => "Pdf";

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(Stream sourceStream)
    {
        using var document = PdfDocument.Open(sourceStream);
        if (document.TryGetBookmarks(out var bookmarks))
        {
            var result =
                ExtractTextBetweenBookmarks(document, bookmarks.GetNodes().OfType<DocumentBookmarkNode>().ToList());

            return await Task.FromResult<IEnumerable<Chapter>>(result);
        }

        return
        [
            new Chapter
            {
                Name = "Root",
                Content = ExtractAllText(document),
            }
        ];
    }

    private static string ExtractAllText(PdfDocument document)
    {
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            builder.Append(page.Text);
        }

        return builder.ToString();
    }

    private List<Chapter> ExtractTextBetweenBookmarks(
        PdfDocument document,
        List<DocumentBookmarkNode> bookmarks)
    {
        var chapters = new List<Chapter>();

        for (int i = 0; i < bookmarks.Count; i++)
        {
            var currentBookmark = bookmarks[i];
            var startPage = currentBookmark.Destination.PageNumber;
            var startY = currentBookmark.Destination.Coordinates.Top ?? 10000;
            int endPage;
            double endY;

            // Определяем конечную страницу для текущей главы:
            // либо следующая закладка, либо конец документа
            if (i < bookmarks.Count - 1)
            {
                endPage = bookmarks[i + 1].Destination.PageNumber;
                // Убедимся, что endPage не меньше startPage
                if (endPage < startPage) endPage = startPage;
                endY = bookmarks[i + 1].Destination.Coordinates.Top ?? 0;
            }
            else
            {
                endPage = document.NumberOfPages;
                endY = 0;
            }

            // Извлекаем текст для диапазона страниц
            string content = ExtractTextFromPageRange(document, startPage, startY, endPage, endY);

            chapters.Add(new Chapter
            {
                Name = currentBookmark.Title,
                Content = content,
            });
        }

        return chapters;
    }

    private string ExtractTextFromPageRange(PdfDocument document, int startPage, double startY, int endPage,
        double endY)
    {
        var text = new StringBuilder();

        for (int pageNum = startPage; pageNum <= endPage; pageNum++)
        {
            if (pageNum > document.NumberOfPages || pageNum < 1)
                continue;
            var page = document.GetPage(pageNum);
            var words = page.GetWords();
            if (pageNum == startPage)
                words = words.Where(e => e.BoundingBox.Top <= startY);
            if (pageNum == endPage)
                words = words.Where(e => e.BoundingBox.Top > endY);
            text.AppendLine(ConcatWords(words));
        }

        return text.ToString();
    }

    private static string ConcatWords(IEnumerable<Word> words)
    {
        var builder = new StringBuilder();
        Word? lastWord = null;
        foreach (var word in words)
        {
            if (lastWord != null && lastWord.BoundingBox.Bottom > word.BoundingBox.Bottom)
                builder.Append('\n');
            else if (lastWord != null)
                builder.Append(' ');
            lastWord = word;
            builder.Append(word);
        }

        return builder.ToString();
    }
}