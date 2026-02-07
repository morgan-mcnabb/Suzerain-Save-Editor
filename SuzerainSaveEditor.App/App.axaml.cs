using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using SuzerainSaveEditor.App.Services;
using SuzerainSaveEditor.App.ViewModels;
using SuzerainSaveEditor.App.Views;
using SuzerainSaveEditor.Core.Parsing;
using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // compose services
            var parser = new JsonSaveParser();
            var backupService = new BackupService();
            var saveFileService = new SaveFileService(parser, backupService);
            var schemaService = new SchemaService();
            var fieldResolver = new FieldResolver();
            var discoveryService = new FieldDiscoveryService(schemaService);

            var mainWindow = new MainWindow();
            var fileDialogService = new FileDialogService(mainWindow);

            mainWindow.DataContext = new MainWindowViewModel(
                saveFileService,
                schemaService,
                fieldResolver,
                fileDialogService,
                discoveryService);

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
