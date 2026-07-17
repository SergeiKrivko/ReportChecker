using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using DynamicData;
using ReactiveUI.Avalonia;
using ReportChecker.Studio.Models;
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

    private IDisposable? _subscription;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        ApplyHighlightingForCurrentFile();
        _subscription?.Dispose();
        _subscription = ViewModel?.Jumps.Subscribe(jump => NavigateToLine(jump.Line ?? 0));
    }

    private void ApplyHighlightingForCurrentFile()
    {
        if (string.IsNullOrEmpty(ViewModel?.Path))
            return;

        var extension = Path.GetExtension(ViewModel?.Path);
        var language = _registryOptions.GetLanguageByExtension(extension);

        if (language != null)
        {
            var scope = _registryOptions.GetScopeByLanguageId(language.Id);
            _textMateInstallation.SetGrammar(scope);
        }
        else
            _textMateInstallation.SetGrammar(null);
    }

    private CompletionWindow? _completionWindow;

    private void Editor_OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (_completionWindow != null)
            return;
        var completions = ViewModel?.GetCompletions(e.Text ?? "", Editor.Text, Editor.TextArea.Caret.Offset);
        if (completions == null || completions.Count == 0)
            return;

        _completionWindow = new CompletionWindow(Editor.TextArea);
        IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;

        // Добавляем элементы автодополнения
        data.AddRange(completions.Completions.Select(c => new CompletionData(c)));
        if (completions.StartOffset >= 0)
        {
            var startOffset = completions.StartOffset;
            var endOffset = completions.EndOffset == -1 ? Editor.TextArea.Caret.Offset : completions.EndOffset;
            _completionWindow.CompletionList.SelectItem(Editor.Text[startOffset..endOffset]);
            _completionWindow.StartOffset = startOffset;
            _completionWindow.EndOffset = endOffset;
        }

        _completionWindow.Show();
        _completionWindow.Closed += delegate { _completionWindow = null; };
    }

    private async void Editor_OnKeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.S && (e.KeyModifiers & KeyModifiers.Control) != 0)
                await ViewModel!.Save();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private async void NavigateToLine(int lineNumber)
    {
        try
        {
            if (Editor.Document == null)
                await Task.Delay(300);
            if (Editor.Document == null)
                return;
            if (lineNumber < 1 || lineNumber > Editor.Document.LineCount)
                return;
            var line = Editor.Document.GetLineByNumber(lineNumber);
            Editor.TextArea.Caret.Offset = line.Offset;
            Editor.TextArea.Caret.BringCaretToView();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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
        var startOffset = completionSegment.Offset + completion.SelectFrom;
        var endOffset = startOffset + completion.SelectLength;

        // Вставляет текст в редактор
        textArea.Document.Replace(completionSegment, completion.Text);
        if (completion.SelectLength > 0)
            textArea.Selection = Selection.Create(textArea, startOffset, endOffset);
        textArea.Caret.Offset = endOffset;
    }
}