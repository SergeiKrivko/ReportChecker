using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AvaluxUI.Controls;
using ReactiveUI;

namespace ReportChecker.Studio.ViewModels;

public class EditorTabViewModel(string path, EditorViewModel editorViewModel, EditorFileViewModel fileViewModel)
    : ViewModelBase
{
    public string Name { get; } = System.IO.Path.GetFileName(path);

    public bool IsSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsModified
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    protected override void OnActivate(CompositeDisposable disposable)
    {
        editorViewModel.SelectedFileObservable
            .Prepend(editorViewModel.SelectedFile)
            .Subscribe(f => IsSelected = f == path)
            .DisposeWith(disposable);
        fileViewModel.ObservableForProperty(e => e.IsModified)
            .Select(e => e.Value)
            .Prepend(fileViewModel.IsModified)
            .Subscribe(e => IsModified = e)
            .DisposeWith(disposable);
    }

    public async void Close()
    {
        try
        {
            if (IsModified)
            {
                switch (await PromptDialog.Prompt($"Сохранить изменения в файле {Name}?", [
                            new PromptDialogButton<int>("Отмена", 0),
                            new PromptDialogButton<int>("Нет", 1, ButtonAppearance.Danger),
                            new PromptDialogButton<int>("Да", 2, ButtonAppearance.Accent),
                        ]))
                {
                    case 0:
                        return;
                    case 1:
                        fileViewModel.DeleteBackup();
                        break;
                    case 2:
                        await fileViewModel.Save();
                        break;
                }
            }

            editorViewModel.CloseFile(path);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void SelectFile()
    {
        editorViewModel.SelectFile(path);
    }
}