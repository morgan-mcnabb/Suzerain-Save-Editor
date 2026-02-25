using System.IO;
using System.Security;
using Avalonia.Controls;
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
    }

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
