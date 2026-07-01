using System;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI.Avalonia;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class FileSystemView : ReactiveUserControl<FileSystemViewModel>
{
    public FileSystemView()
    {
        InitializeComponent();
    }

    private void Item_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var entry = (sender as Control)?.DataContext as IProjectFileSystemEntry;
        if (entry == null)
            return;
        ViewModel?.OpenFile(entry.Path);
    }
}