using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SuzerainSaveEditor.App.Views;

public enum UnsavedChangesResult
{
    Cancel,
    Discard,
    Save
}

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesResult Result { get; private set; } = UnsavedChangesResult.Cancel;

    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = UnsavedChangesResult.Cancel;
        Close();
    }

    private void OnDiscard(object? sender, RoutedEventArgs e)
    {
        Result = UnsavedChangesResult.Discard;
        Close();
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        Result = UnsavedChangesResult.Save;
        Close();
    }
}
