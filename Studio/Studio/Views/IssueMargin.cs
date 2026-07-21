using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.Views;

public class IssueMargin : AbstractMargin
{
    private readonly TextEditor _editor;
    private readonly IIssueService _issueService;
    private IReadOnlyList<FileIssue> _issues = [];

    public IssueMargin(TextEditor editor, IObservable<IReadOnlyList<FileIssue>> issuesObservable, IIssueService issueService)
    {
        _editor = editor;
        _issueService = issueService;
        IsHitTestVisible = true;
        Width = _editor.FontSize;

        // Подписка на события
        editor.TextArea.TextView.VisualLinesChanged += TextViewOnVisualLinesChanged;
        issuesObservable.Subscribe(e =>
        {
            _issues = e;
            InvalidateVisual();
        });
    }

    private void TextViewOnVisualLinesChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext drawingContext)
    {
        base.Render(drawingContext);

        var textView = _editor.TextArea.TextView;
        if (_editor.Document == null)
            return;

        foreach (var issue in _issues)
        {
            // Получаем позицию строки
            var documentLine = _editor.Document.GetLineByNumber(issue.Position?.Line ?? 1);
            var linePosition = textView.GetVisualPosition(new TextViewPosition(documentLine.LineNumber, 1),
                VisualYPosition.LineMiddle) - textView.ScrollOffset;

            // Проверяем, видна ли строка
            if (linePosition.Y < 0 || linePosition.Y > Bounds.Height)
                continue;

            // Рисуем маркер
            Rect rect = new Rect(2, linePosition.Y - 5, 10, 10);
            drawingContext.DrawEllipse(Brushes.Red, new Pen(Brushes.DarkRed, 1), rect.Position + new Vector(5, 5), 5,
                5);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var position = e.GetPosition(_editor.TextArea.TextView);
        var line = GetLineFromY(position.Y);
        var issue = _issues.FirstOrDefault(i => i.Position?.Line == line);
        _issueService.SelectIssue(issue);
    }

    private int? GetLineFromY(double y)
    {
        var textView = _editor.TextArea.TextView;
        var line = textView.GetDocumentLineByVisualTop(y + textView.ScrollOffset.Y);
        return line.LineNumber;
    }
}