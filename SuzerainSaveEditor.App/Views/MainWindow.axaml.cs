using System.IO;
using System.Security;
using Avalonia.Controls;
using Avalonia.Input;
using SuzerainSaveEditor.App.Services;
using SuzerainSaveEditor.App.ViewModels;

namespace SuzerainSaveEditor.App.Views;

public partial class MainWindow : Window
{
    private bool _forceClose;
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;

        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        var valid = HasJsonFile(e);
        e.DragEffects = valid ? DragDropEffects.Copy : DragDropEffects.None;

        var overlay = this.FindControl<Border>("DropOverlay");
        if (overlay is not null) overlay.IsVisible = valid;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = HasJsonFile(e) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        var overlay = this.FindControl<Border>("DropOverlay");
        if (overlay is not null) overlay.IsVisible = false;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var overlay = this.FindControl<Border>("DropOverlay");
        if (overlay is not null) overlay.IsVisible = false;

        var path = GetFirstJsonFilePath(e);
        if (path is null) return;

        if (DataContext is not MainWindowViewModel vm) return;

        if (vm.IsDirty && !vm.SaveCommittedToDisk)
        {
            var dialog = new UnsavedChangesDialog();
            await dialog.ShowDialog(this);

            switch (dialog.Result)
            {
                case UnsavedChangesResult.Save:
                    try
                    {
                        await vm.SaveCommand.ExecuteAsync(null);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
                    {
                        // save failed — don't load the dropped file
                        return;
                    }

                    if (vm.IsDirty && !vm.SaveCommittedToDisk)
                        return; // save didn't clear dirty state
                    break;

                case UnsavedChangesResult.Discard:
                    break;

                case UnsavedChangesResult.Cancel:
                    return;
            }
        }

        await vm.LoadFileAsync(path);
    }

    private static bool HasJsonFile(DragEventArgs e)
        => DragDropHelper.HasJsonFile(DragDropHelper.ToDroppedFiles(e.DataTransfer.TryGetFiles()));

    private static string? GetFirstJsonFilePath(DragEventArgs e)
        => DragDropHelper.GetFirstJsonFilePath(DragDropHelper.ToDroppedFiles(e.DataTransfer.TryGetFiles()));

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose) return;

        if (DataContext is not MainWindowViewModel vm) return;
        if (!vm.IsDirty || vm.SaveCommittedToDisk) return;

        e.Cancel = true;

        // prevent a second dialog if the user hits Alt+F4 while the first is open
        if (_isClosing) return;
        _isClosing = true;

        try
        {
            var dialog = new UnsavedChangesDialog();
            await dialog.ShowDialog(this);

            switch (dialog.Result)
            {
                case UnsavedChangesResult.Save:
                    try
                    {
                        await vm.SaveCommand.ExecuteAsync(null);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
                    {
                        // save failed — keep window open so user can retry or discard
                        break;
                    }

                    if (!vm.IsDirty || vm.SaveCommittedToDisk)
                    {
                        _forceClose = true;
                        Close();
                    }
                    break;

                case UnsavedChangesResult.Discard:
                    _forceClose = true;
                    Close();
                    break;

                case UnsavedChangesResult.Cancel:
                    break;
            }
        }
        finally
        {
            _isClosing = false;
        }
    }
}
