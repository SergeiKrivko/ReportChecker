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

    public IssueMargin(TextEditor editor, IObservable<IReadOnlyList<FileIssue>> issuesObservable,
        IIssueService issueService)
    {
        _editor = editor;
        _issueService = issueService;
        IsHitTestVisible = true;
        Width = _editor.FontSize + 4;

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
        var width = _editor.FontSize;
        if (_editor.Document == null)
            return;

        Application.Current!.TryGetResource("CanvasColor", ActualThemeVariant, out var backgroundColor);
        IBrush backgroundBrush = backgroundColor is Color c ? new SolidColorBrush(c) : Brushes.Gray;

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
            Rect rect = new Rect(2, linePosition.Y - width / 2, width, width);
            drawingContext.DrawRectangle(backgroundBrush, null, rect);
            var icon = GetIssueIcon(issue.Issue)?.Clone();
            var brush = GetIssueIconBrush(issue.Issue);
            icon?.Transform = CreateIconTransform(icon, rect);
            if (icon != null)
                drawingContext.DrawGeometry(brush, null, icon);
        }
    }

    private Geometry? GetIssueIcon(Issue issue)
    {
        var key = GetIconKey(issue);
        if (Application.Current?.Resources.TryGetResource(key, Application.Current.ActualThemeVariant,
                out var resource) ?? false)
            return resource as Geometry;
        return null;
    }

    private static string GetIconKey(Issue issue)
    {
        switch (issue.Status)
        {
            case IssueStatus.Open:
                if (issue.Priority >= 1 && issue.Priority <= 2)
                    return "IconShieldAlert";
                if (issue.Priority >= 3 && issue.Priority <= 5)
                    return "IconTriangleAlert";
                return "IconCircleAlert";
            case IssueStatus.Closed:
                return "IconClose";
            case IssueStatus.Fixed:
                return "IconCheckmark";
        }

        return "IconHelp";
    }

    private IBrush? GetIssueIconBrush(Issue issue)
    {
        var key = issue.Status switch
        {
            IssueStatus.Open => issue.Priority switch
            {
                1 => "DangerColor",
                2 => "DangerColor",
                3 => "WarningColor",
                4 => "WarningColor",
                5 => "WarningColor",
                _ => "PrimaryColor",
            },
            IssueStatus.Closed => "BorderColor",
            IssueStatus.Fixed => "SuccessColor",
            _ => ""
        };
        if (Application.Current?.Resources.TryGetResource(key, Application.Current.ActualThemeVariant,
                out var resource) == true && resource is Color color)
        {
            IBrush brush = new SolidColorBrush(color);
            return brush;
        }

        return null;
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

    private Transform CreateIconTransform(Geometry icon, Rect rect)
    {
        var scale = rect.Width / icon.Bounds.Width;
        return new TransformGroup()
        {
            Children =
            [
                new ScaleTransform(scale, scale),
                new TranslateTransform(rect.X - icon.Bounds.X, rect.Y - icon.Bounds.Y)
            ]
        };
    }
}