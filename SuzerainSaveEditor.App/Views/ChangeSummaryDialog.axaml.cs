using Avalonia.Controls;
using Avalonia.Interactivity;
using SuzerainSaveEditor.App.ViewModels;

namespace SuzerainSaveEditor.App.Views;

public enum ChangeSummaryResult
{
    Cancel,
    Save
}

public partial class ChangeSummaryDialog : Window
{
    public ChangeSummaryResult Result { get; private set; } = ChangeSummaryResult.Cancel;

    public ChangeSummaryDialog()
    {
        InitializeComponent();
    }

    public void SetChanges(IReadOnlyList<ChangeSummaryItemViewModel> items)
    {
        var count = items.Count;
        SummaryTextBlock.Text = count == 1
            ? "1 unsaved change"
            : $"{count} unsaved changes";

        ChangesList.ItemsSource = items;
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = ChangeSummaryResult.Cancel;
        Close();
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        Result = ChangeSummaryResult.Save;
        Close();
    }
}
