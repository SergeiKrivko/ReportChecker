using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using DynamicData;
using ReactiveUI.Avalonia;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.ViewModels;
using TextMateSharp.Grammars;

namespace ReportChecker.Studio.Views;

public partial class EditorFileView : ReactiveUserControl<EditorFileViewModel>
{
    private readonly TextMate.Installation _textMateInstallation;
    private readonly RegistryOptions _registryOptions;

    public EditorFileView()
    {
        InitializeComponent();

        _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        _textMateInstallation = Editor.InstallTextMate(_registryOptions);
        Editor.TextArea.TextEntered += Editor_OnTextInput;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        ApplyHighlightingForCurrentFile();
    }

    private void ApplyHighlightingForCurrentFile()
    {
        if (string.IsNullOrEmpty(ViewModel?.Path))
            return;

        // Получаем расширение файла
        var extension = Path.GetExtension(ViewModel?.Path);

        // TextMate использует расширение с точкой или без неё,
        // но RegistryOptions.GetLanguageByExtension ожидает расширение с точкой
        var language = _registryOptions.GetLanguageByExtension(extension);

        if (language != null)
        {
            var scope = _registryOptions.GetScopeByLanguageId(language.Id);
            _textMateInstallation.SetGrammar(scope);
        }
        else
        {
            // Если язык не найден — отключаем подсветку
            _textMateInstallation.SetGrammar(null);
        }
    }

    private CompletionWindow? _completionWindow;

    private void Editor_OnTextInput(object? sender, TextInputEventArgs e)
    {
        var completions = ViewModel?.GetCompletions(e.Text ?? "");
        if (completions == null || completions.Count == 0)
            return;

        _completionWindow = new CompletionWindow(Editor.TextArea);
        IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;

        // Добавляем элементы автодополнения
        data.AddRange(completions.Select(c => new CompletionData(c)));

        _completionWindow.Show();
        _completionWindow.Closed += delegate { _completionWindow = null; };
    }
}

internal class CompletionData(ILanguageCompletion completion) : ICompletionData
{
    public string Text => completion.Name;

    // Отображаемый контент (может быть UIElement)
    public object Content => Text;

    // Всплывающая подсказка
    public object Description => completion.Description ?? "";

    public double Priority { get; } = 0;
    public IImage? Image => null;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        // Вставляет текст в редактор
        textArea.Document.Replace(completionSegment, completion.Text);
        if (completion.SelectLength > 0)
        {
            var startOffset = completionSegment.Offset + completion.SelectFrom;
            var endOffset = startOffset + completion.SelectLength;

            textArea.Selection = Selection.Create(textArea, startOffset, endOffset);
            textArea.Caret.Offset = endOffset;
        }
    }
}