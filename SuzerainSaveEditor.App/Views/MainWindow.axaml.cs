using Avalonia.Controls;
using SuzerainSaveEditor.App.ViewModels;

namespace SuzerainSaveEditor.App.Views;

public partial class MainWindow : Window
{
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose) return;

        if (DataContext is not MainWindowViewModel { IsDirty: true } vm) return;

        e.Cancel = true;

        var dialog = new UnsavedChangesDialog();
        await dialog.ShowDialog(this);

        switch (dialog.Result)
        {
            case UnsavedChangesResult.Save:
                try
                {
                    await vm.SaveCommand.ExecuteAsync(null);
                }
                catch
                {
                    // save failed — keep window open so user can retry or discard
                    break;
                }

                if (!vm.IsDirty)
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
}
