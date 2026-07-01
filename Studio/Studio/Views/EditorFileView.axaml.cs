using System;
using System.IO;
using AvaloniaEdit.TextMate;
using ReactiveUI.Avalonia;
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
}