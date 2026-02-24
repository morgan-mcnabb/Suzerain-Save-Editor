using System.Text.Json.Nodes;
using SuzerainSaveEditor.App.Services;
using SuzerainSaveEditor.App.ViewModels;
using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    private readonly ISchemaService _schema = new SchemaService();
    private readonly IFieldResolver _resolver = new FieldResolver();

    // builds a save document with default values for every field referenced by the schema
    private SaveDocument CreateTestDocument()
    {
        var variables = new List<LuaVariable>();
        var entities = new List<EntityUpdate>();

        foreach (var field in _schema.GetAll())
        {
            switch (field.Source)
            {
                case FieldSource.Variable:
                {
                    var key = field.Path["variable:".Length..];
                    LuaValue value = field.Type switch
                    {
                        FieldType.Bool => new LuaValue.Bool(false),
                        FieldType.Int => new LuaValue.Int(5),
                        FieldType.String => new LuaValue.Str("test_value"),
                        FieldType.Enum => new LuaValue.Str("option_a"),
                        _ => new LuaValue.Str("")
                    };
                    variables.Add(new LuaVariable(key, value));
                    break;
                }
                case FieldSource.EntityUpdate:
                {
                    var entityPath = field.Path["entity:".Length..];
                    var lastDot = entityPath.LastIndexOf('.');
                    var nameInDb = entityPath[..lastDot];
                    var fieldName = entityPath[(lastDot + 1)..];
                    entities.Add(new EntityUpdate(nameInDb, fieldName, "0"));
                    break;
                }
            }
        }

        var metadata = new SaveMetadata(
            SaveFileType: 0,
            CampaignName: "PRESIDENT",
            CurrentStoryPack: "BaseGame",
            TurnNo: 5,
            SaveFileName: "test_save",
            SceneBuildIndex: 1,
            LastModified: "2024-01-01",
            Version: "1.3.2",
            IsVersionMismatched: false,
            IsTorporModeOn: false,
            Notes: "Test notes");

        return new SaveDocument
        {
            Metadata = metadata,
            WarSaveData = new JsonObject(),
            Variables = variables,
            EntityUpdates = entities
        };
    }

    private MainWindowViewModel CreateViewModel(
        ISaveFileService? saveFileService = null,
        IFileDialogService? fileDialogService = null,
        IFieldDiscoveryService? discoveryService = null)
    {
        var doc = CreateTestDocument();
        saveFileService ??= new FakeSaveFileService(doc);
        fileDialogService ??= new FakeFileDialogService("C:\\saves\\test.json");
        discoveryService ??= new FakeFieldDiscoveryService();

        return new MainWindowViewModel(saveFileService, _schema, _resolver, fileDialogService, discoveryService);
    }

    // helper to find a field across all category nodes (recursive)
    private static FieldViewModel? FindAdvancedField(MainWindowViewModel vm, string fieldId)
    {
        return FindFieldInNodes(vm.CategoryNodes, fieldId);
    }

    private static FieldViewModel? FindFieldInNodes(
        IEnumerable<CategoryNodeViewModel> nodes, string fieldId)
    {
        foreach (var node in nodes)
        {
            var found = node.AllFields.FirstOrDefault(f => f.FieldId == fieldId);
            if (found is not null) return found;

            found = FindFieldInNodes(node.Children, fieldId);
            if (found is not null) return found;
        }
        return null;
    }

    // helper to count all fields across all category nodes (recursive)
    private static int CountAdvancedFields(MainWindowViewModel vm)
    {
        return CountFieldsInNodes(vm.CategoryNodes);
    }

    private static int CountFieldsInNodes(IEnumerable<CategoryNodeViewModel> nodes)
    {
        var count = 0;
        foreach (var node in nodes)
        {
            count += node.AllFields.Count;
            count += CountFieldsInNodes(node.Children);
        }
        return count;
    }

    // initial state
    [Fact]
    public void InitialState_NotLoaded_NotDirty()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsFileLoaded);
        Assert.False(vm.IsDirty);
        Assert.False(vm.IsLoading);
        Assert.Equal("", vm.FilePath);
        Assert.Equal("Suzerain Save Editor", vm.WindowTitle);
        Assert.Empty(vm.GeneralFields);
        Assert.Empty(vm.SordlandFields);
        Assert.Empty(vm.RiziaFields);
        Assert.Empty(vm.CategoryNodes);
    }

    // open command
    [Fact]
    public async Task OpenCommand_PopulatesFields()
    {
        var vm = CreateViewModel();

        await vm.OpenCommand.ExecuteAsync(null);

        Assert.True(vm.IsFileLoaded);
        Assert.False(vm.IsDirty);
        Assert.Equal("C:\\saves\\test.json", vm.FilePath);
        Assert.NotEmpty(vm.GeneralFields);
        Assert.NotEmpty(vm.SordlandFields);
        Assert.NotEmpty(vm.RiziaFields);
    }

    [Fact]
    public async Task OpenCommand_SetsCorrectFieldCount()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.Equal(6, vm.GeneralFields.Count);
        Assert.Equal(39, vm.SordlandFields.Count);
        Assert.Equal(97, vm.RiziaFields.Count);
    }

    [Fact]
    public async Task OpenCommand_GeneralFields_HaveCorrectValues()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        var campaignName = vm.GeneralFields.First(f => f.FieldId == "meta.campaignName");
        Assert.Equal("PRESIDENT", campaignName.Value);
        Assert.Equal("Campaign Name", campaignName.Label);
        Assert.Equal(FieldType.String, campaignName.FieldType);

        var turnNo = vm.GeneralFields.First(f => f.FieldId == "meta.turnNo");
        Assert.Equal("5", turnNo.Value);
        Assert.Equal(FieldType.Int, turnNo.FieldType);
        Assert.Equal(1, turnNo.Min);
        Assert.Equal(20, turnNo.Max);
    }

    [Fact]
    public async Task OpenCommand_CancelledDialog_NoChange()
    {
        var vm = CreateViewModel(fileDialogService: new FakeFileDialogService(null));

        await vm.OpenCommand.ExecuteAsync(null);

        Assert.False(vm.IsFileLoaded);
        Assert.Empty(vm.GeneralFields);
    }

    [Fact]
    public async Task OpenCommand_WindowTitle_IncludesFileName()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.Contains("test.json", vm.WindowTitle);
        Assert.DoesNotContain("*", vm.WindowTitle);
    }

    // field editing
    [Fact]
    public async Task FieldValueChange_UpdatesDirtyState()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        var field = vm.GeneralFields.First(f => f.FieldId == "meta.campaignName");
        field.Value = "NEW_CAMPAIGN";

        Assert.True(vm.IsDirty);
        Assert.True(field.IsDirty);
        Assert.Equal(1, vm.ChangeCount);
        Assert.Equal("1 unsaved change", vm.ChangeCountText);
    }

    [Fact]
    public async Task FieldValueChange_InvalidValue_ShowsValidationError()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        var turnNo = vm.GeneralFields.First(f => f.FieldId == "meta.turnNo");
        turnNo.Value = "not_a_number";

        Assert.NotNull(turnNo.ValidationError);
        Assert.True(turnNo.HasValidationError);
    }

    [Fact]
    public async Task FieldValueChange_ResetToSameValue_NotDirty()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        var field = vm.GeneralFields.First(f => f.FieldId == "meta.campaignName");
        var original = field.Value;
        field.Value = "CHANGED";
        Assert.True(vm.IsDirty);

        field.Value = original;
        Assert.False(vm.IsDirty);
        Assert.False(field.IsDirty);
    }

    [Fact]
    public async Task MultipleFieldChanges_CorrectChangeCount()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "NEW";
        vm.GeneralFields.First(f => f.FieldId == "meta.notes").Value = "new notes";

        Assert.Equal(2, vm.ChangeCount);
        Assert.Equal("2 unsaved changes", vm.ChangeCountText);
    }

    [Fact]
    public async Task WindowTitle_DirtyIndicator()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.DoesNotContain("*", vm.WindowTitle);

        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";

        Assert.Contains("*", vm.WindowTitle);
    }

    // revert
    [Fact]
    public async Task RevertAll_RestoresOriginalValues()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        var field = vm.GeneralFields.First(f => f.FieldId == "meta.campaignName");
        var original = field.Value;
        field.Value = "CHANGED";
        Assert.True(vm.IsDirty);

        vm.RevertAllCommand.Execute(null);

        Assert.False(vm.IsDirty);
        Assert.Equal(original, field.Value);
        Assert.False(field.IsDirty);
        Assert.Equal(0, vm.ChangeCount);
    }

    [Fact]
    public void RevertAllCommand_DisabledWhenNotDirty()
    {
        var vm = CreateViewModel();
        Assert.False(vm.RevertAllCommand.CanExecute(null));
    }

    [Fact]
    public async Task RevertAllCommand_EnabledWhenDirty()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";
        Assert.True(vm.RevertAllCommand.CanExecute(null));
    }

    // save
    [Fact]
    public async Task SaveCommand_DisabledWhenNotDirty()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_EnabledWhenDirty()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";
        Assert.True(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_InvokesSaveService()
    {
        var fakeSave = new FakeSaveFileService(CreateTestDocument());
        var vm = CreateViewModel(saveFileService: fakeSave);
        await vm.OpenCommand.ExecuteAsync(null);

        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Single(fakeSave.SaveCalls);
        Assert.Equal("C:\\saves\\test.json", fakeSave.SaveCalls[0].Path);
    }

    [Fact]
    public async Task SaveCommand_ResetsDirtyStateAfterSave()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";
        Assert.True(vm.IsDirty);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.IsDirty);
        Assert.Equal(0, vm.ChangeCount);
    }

    [Fact]
    public async Task SaveCommand_PreservesTabSelection()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.SelectedGroupIndex = 2; // rizia
        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.SelectedGroupIndex);
    }

    [Fact]
    public async Task SaveCommand_FailingService_ShowsError()
    {
        var failingSave = new FailingSaveFileService(CreateTestDocument());
        var vm = CreateViewModel(saveFileService: failingSave);
        await vm.OpenCommand.ExecuteAsync(null);

        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Save failed", vm.StatusMessage);
    }

    // search
    [Fact]
    public async Task Search_FiltersFieldsByLabel()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.SearchText = "Budget";

        // "Government Budget" in Sordland should match
        Assert.Contains(vm.SordlandFields, f => f.Label.Contains("Budget"));
        // all visible fields should contain "Budget" in label/id/description
        Assert.All(vm.SordlandFields, f =>
            Assert.True(
                f.Label.Contains("Budget", StringComparison.OrdinalIgnoreCase) ||
                f.FieldId.Contains("Budget", StringComparison.OrdinalIgnoreCase) ||
                (f.Description?.Contains("Budget", StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    [Fact]
    public async Task Search_EmptyQuery_ShowsAllFields()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.SearchText = "Budget";
        var filteredCount = vm.SordlandFields.Count;

        vm.SearchText = "";

        Assert.Equal(39, vm.SordlandFields.Count);
        Assert.True(vm.SordlandFields.Count > filteredCount);
    }

    [Fact]
    public async Task Search_CaseInsensitive()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.SearchText = "CAMPAIGN";
        Assert.Contains(vm.GeneralFields, f => f.FieldId == "meta.campaignName");
    }

    [Fact]
    public async Task Search_ByDescription()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.SearchText = "faction";
        // several Sordland opinion fields have "faction" in their descriptions
        Assert.NotEmpty(vm.SordlandFields);
    }

    [Fact]
    public async Task Search_PreservedAfterSave()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        // make edit before filtering so the field is still accessible
        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";
        vm.SearchText = "Budget";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Budget", vm.SearchText);
    }

    // status messages
    [Fact]
    public async Task StatusMessage_ShowsLoadedFileName()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.Contains("test.json", vm.StatusMessage);
    }

    [Fact]
    public async Task StatusMessage_ShowsSaveSuccess()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        vm.GeneralFields.First(f => f.FieldId == "meta.campaignName").Value = "CHANGED";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Saved successfully", vm.StatusMessage);
    }

    // bool field binding
    [Fact]
    public async Task BoolField_ToggleSwitch_UpdatesSession()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        var torpor = vm.GeneralFields.First(f => f.FieldId == "meta.isTorporModeOn");
        Assert.False(torpor.BoolValue); // initial: False

        torpor.BoolValue = true;

        Assert.True(vm.IsDirty);
        Assert.True(torpor.IsDirty);
        Assert.Equal("True", torpor.Value);
    }

    // advanced tab (discovered fields)
    private static IReadOnlyList<FieldDefinition> CreateDiscoveredFields()
    {
        return
        [
            new FieldDefinition
            {
                Id = "discovered.var.Custom.TestBool",
                Path = "variable:Custom.TestBool",
                Label = "Test Bool",
                Group = FieldGroup.Advanced,
                Type = FieldType.Bool,
                Source = FieldSource.Variable,
                Description = "Variable: Custom.TestBool"
            },
            new FieldDefinition
            {
                Id = "discovered.var.Custom.TestInt",
                Path = "variable:Custom.TestInt",
                Label = "Test Int",
                Group = FieldGroup.Advanced,
                Type = FieldType.Int,
                Source = FieldSource.Variable,
                Description = "Variable: Custom.TestInt"
            },
            new FieldDefinition
            {
                Id = "discovered.entity.Custom_Ent.Score",
                Path = "entity:Custom_Ent.Score",
                Label = "Score",
                Group = FieldGroup.Advanced,
                Type = FieldType.String,
                Source = FieldSource.EntityUpdate,
                Description = "Entity: Custom_Ent.Score"
            }
        ];
    }

    private static IReadOnlyList<FieldDefinition> CreateDiscoveredFieldsWithTurns()
    {
        return
        [
            new FieldDefinition
            {
                Id = "discovered.var.GameCondition.Turn01_A_PoliticalOverview",
                Path = "variable:GameCondition.Turn01_A_PoliticalOverview",
                Label = "Political Overview",
                Group = FieldGroup.Advanced,
                Type = FieldType.Bool,
                Source = FieldSource.Variable,
                Description = "Turn 01 > General | GameCondition.Turn01_A_PoliticalOverview"
            },
            new FieldDefinition
            {
                Id = "discovered.var.GameCondition.Turn01_EnT_BudgetOne",
                Path = "variable:GameCondition.Turn01_EnT_BudgetOne",
                Label = "Budget One",
                Group = FieldGroup.Advanced,
                Type = FieldType.Bool,
                Source = FieldSource.Variable,
                Description = "Turn 01 > Economy & Trade | GameCondition.Turn01_EnT_BudgetOne"
            },
            new FieldDefinition
            {
                Id = "discovered.var.GameCondition.Turn02_FPnT_DiplomacyOverview",
                Path = "variable:GameCondition.Turn02_FPnT_DiplomacyOverview",
                Label = "Diplomacy Overview",
                Group = FieldGroup.Advanced,
                Type = FieldType.Bool,
                Source = FieldSource.Variable,
                Description = "Turn 02 > Foreign Policy & Treaties | GameCondition.Turn02_FPnT_DiplomacyOverview"
            },
            new FieldDefinition
            {
                Id = "discovered.var.Custom.TestBool",
                Path = "variable:Custom.TestBool",
                Label = "Test Bool",
                Group = FieldGroup.Advanced,
                Type = FieldType.Bool,
                Source = FieldSource.Variable,
                Description = "Variable: Custom.TestBool"
            },
            new FieldDefinition
            {
                Id = "discovered.entity.Custom_Ent.Score",
                Path = "entity:Custom_Ent.Score",
                Label = "Score",
                Group = FieldGroup.Advanced,
                Type = FieldType.String,
                Source = FieldSource.EntityUpdate,
                Description = "Entity: Custom_Ent.Score"
            }
        ];
    }

    private MainWindowViewModel CreateViewModelWithDiscoveredFields(
        IReadOnlyList<FieldDefinition>? discoveredFields = null)
    {
        var discovered = discoveredFields ?? CreateDiscoveredFields();

        // build a document that includes both schema fields AND the discovered ones
        var variables = new List<LuaVariable>();
        var entities = new List<EntityUpdate>();

        foreach (var field in _schema.GetAll())
        {
            switch (field.Source)
            {
                case FieldSource.Variable:
                {
                    var key = field.Path["variable:".Length..];
                    LuaValue value = field.Type switch
                    {
                        FieldType.Bool => new LuaValue.Bool(false),
                        FieldType.Int => new LuaValue.Int(5),
                        FieldType.String => new LuaValue.Str("test_value"),
                        FieldType.Enum => new LuaValue.Str("option_a"),
                        _ => new LuaValue.Str("")
                    };
                    variables.Add(new LuaVariable(key, value));
                    break;
                }
                case FieldSource.EntityUpdate:
                {
                    var entityPath = field.Path["entity:".Length..];
                    var lastDot = entityPath.LastIndexOf('.');
                    var nameInDb = entityPath[..lastDot];
                    var fieldName = entityPath[(lastDot + 1)..];
                    entities.Add(new EntityUpdate(nameInDb, fieldName, "0"));
                    break;
                }
            }
        }

        // add discovered variable/entity entries to the document
        foreach (var d in discovered)
        {
            if (d.Source == FieldSource.Variable)
            {
                var key = d.Path["variable:".Length..];
                LuaValue value = d.Type switch
                {
                    FieldType.Bool => new LuaValue.Bool(true),
                    FieldType.Int => new LuaValue.Int(42),
                    FieldType.String => new LuaValue.Str("test_value"),
                    _ => new LuaValue.Str("")
                };
                variables.Add(new LuaVariable(key, value));
            }
            else if (d.Source == FieldSource.EntityUpdate)
            {
                var entityPath = d.Path["entity:".Length..];
                var lastDot = entityPath.LastIndexOf('.');
                var nameInDb = entityPath[..lastDot];
                var fieldName = entityPath[(lastDot + 1)..];
                entities.Add(new EntityUpdate(nameInDb, fieldName, "100"));
            }
        }

        var doc = new SaveDocument
        {
            Metadata = new SaveMetadata(0, "PRESIDENT", "BaseGame", 5, "test_save", 1,
                "2024-01-01", "1.3.2", false, false, "Test notes"),
            WarSaveData = new JsonObject(),
            Variables = variables,
            EntityUpdates = entities
        };

        var saveFileService = new FakeSaveFileService(doc);
        var fileDialogService = new FakeFileDialogService("C:\\saves\\test.json");
        var discoveryService = new FakeFieldDiscoveryService(discovered);

        return new MainWindowViewModel(saveFileService, _schema, _resolver, fileDialogService, discoveryService);
    }

    [Fact]
    public async Task AdvancedTab_PopulatedWithDiscoveredFields()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.Equal(3, CountAdvancedFields(vm));
        Assert.Equal(3, vm.AdvancedFieldCount);
    }

    [Fact]
    public async Task AdvancedTab_NoDiscoveredFields_Empty()
    {
        var vm = CreateViewModel();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.Empty(vm.CategoryNodes);
        Assert.Equal(0, vm.AdvancedFieldCount);
    }

    [Fact]
    public async Task AdvancedTab_SchemaFieldCountsUnchanged()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.Equal(6, vm.GeneralFields.Count);
        Assert.Equal(39, vm.SordlandFields.Count);
        Assert.Equal(97, vm.RiziaFields.Count);
    }

    [Fact]
    public async Task AdvancedTab_EditField_UpdatesDirtyState()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        var boolField = FindAdvancedField(vm, "discovered.var.Custom.TestBool");
        Assert.NotNull(boolField);
        boolField.BoolValue = false;

        Assert.True(vm.IsDirty);
        Assert.True(boolField.IsDirty);
    }

    [Fact]
    public async Task AdvancedTab_RevertAll_ResetsAdvancedFields()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        var boolField = FindAdvancedField(vm, "discovered.var.Custom.TestBool");
        Assert.NotNull(boolField);
        var original = boolField.Value;
        boolField.BoolValue = !boolField.BoolValue;
        Assert.True(vm.IsDirty);

        vm.RevertAllCommand.Execute(null);

        Assert.False(vm.IsDirty);
        Assert.Equal(original, boolField.Value);
    }

    [Fact]
    public async Task AdvancedTab_FieldValues_CorrectFromDocument()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        var boolField = FindAdvancedField(vm, "discovered.var.Custom.TestBool");
        Assert.NotNull(boolField);
        Assert.Equal("True", boolField.Value);

        var intField = FindAdvancedField(vm, "discovered.var.Custom.TestInt");
        Assert.NotNull(intField);
        Assert.Equal("42", intField.Value);

        var entityField = FindAdvancedField(vm, "discovered.entity.Custom_Ent.Score");
        Assert.NotNull(entityField);
        Assert.Equal("100", entityField.Value);
    }

    // category tree structure
    [Fact]
    public async Task AdvancedTab_CategoryNodesGroupedCorrectly()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        // should have category nodes for: Turns (unified), entity:Custom_Ent
        Assert.True(vm.CategoryNodes.Count >= 2);

        // unified Turns node should contain turn fields
        var turns = vm.CategoryNodes.FirstOrDefault(n => n.Key == "Turns");
        Assert.NotNull(turns);
    }

    [Fact]
    public async Task AdvancedTab_CategoryNodesStartCollapsed()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.All(vm.CategoryNodes, n => Assert.False(n.IsExpanded));
    }

    [Fact]
    public async Task AdvancedTab_SearchFiltersCategoryTree()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        vm.SearchText = "Diplomacy";

        // only nodes with matching fields should be visible
        Assert.True(vm.CategoryNodes.Count >= 1);
        var turns = vm.CategoryNodes.FirstOrDefault(n => n.Key == "Turns");
        Assert.NotNull(turns);
    }

    [Fact]
    public async Task AdvancedTab_SearchExpandsMatchingNodes()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        vm.SearchText = "Diplomacy";

        var turns = vm.CategoryNodes.FirstOrDefault(n => n.Key == "Turns");
        Assert.NotNull(turns);
        Assert.True(turns.IsExpanded);
    }

    [Fact]
    public async Task AdvancedTab_CategoryNodeHeaderShowsCount()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        var turns = vm.CategoryNodes.FirstOrDefault(n => n.Key == "Turns");
        Assert.NotNull(turns);
        Assert.Contains("Turns", turns.HeaderText);
    }

    // category selection
    [Fact]
    public async Task SelectCategory_PopulatesSelectedCategoryFields()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        // find a leaf node to select
        var leafNode = vm.CategoryNodes.FirstOrDefault(n => n.IsLeaf && n.AllFields.Count > 0);
        Assert.NotNull(leafNode);

        vm.SelectCategory(leafNode);

        Assert.True(vm.HasCategorySelected);
        Assert.NotEmpty(vm.SelectedCategoryFields);
        Assert.NotEmpty(vm.SelectedCategoryPath);
    }

    [Fact]
    public async Task SelectCategory_Null_ClearsSelection()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        var leafNode = vm.CategoryNodes.FirstOrDefault(n => n.IsLeaf && n.AllFields.Count > 0);
        vm.SelectCategory(leafNode);
        Assert.True(vm.HasCategorySelected);

        vm.SelectCategory(null);

        Assert.False(vm.HasCategorySelected);
        Assert.Empty(vm.SelectedCategoryFields);
    }

    [Fact]
    public async Task AdvancedTab_InitialState_NoCategorySelected()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.False(vm.HasCategorySelected);
        Assert.Empty(vm.SelectedCategoryFields);
        Assert.Equal("", vm.SelectedCategoryPath);
    }

    // card dashboard (parent node selection)

    [Fact]
    public async Task SelectCategory_ParentNode_ShowsCategoryCards()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        var parentNode = vm.CategoryNodes.FirstOrDefault(n => n.IsParent);
        if (parentNode is null) return; // skip if no parent nodes in this dataset

        vm.SelectCategory(parentNode);

        Assert.True(vm.ShowCategoryCards);
        Assert.False(vm.ShowCategoryFields);
        Assert.NotEmpty(vm.SubCategorySummaries);
    }

    [Fact]
    public async Task SelectCategory_LeafNode_ShowsCategoryFields()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        var leafNode = vm.CategoryNodes.FirstOrDefault(n => n.IsLeaf && n.AllFields.Count > 0);
        Assert.NotNull(leafNode);

        vm.SelectCategory(leafNode);

        Assert.False(vm.ShowCategoryCards);
        Assert.True(vm.ShowCategoryFields);
        Assert.NotEmpty(vm.SelectedCategoryFields);
        Assert.Empty(vm.SubCategorySummaries);
    }

    [Fact]
    public async Task SelectCategory_Null_ClearsCardsAndFields()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        // select something first
        var node = vm.CategoryNodes.First();
        vm.SelectCategory(node);

        vm.SelectCategory(null);

        Assert.False(vm.ShowCategoryCards);
        Assert.False(vm.ShowCategoryFields);
        Assert.Empty(vm.SubCategorySummaries);
        Assert.Empty(vm.SelectedCategoryFields);
    }

    [Fact]
    public async Task SelectCategory_ParentNode_AutoExpands()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        var parentNode = vm.CategoryNodes.FirstOrDefault(n => n.IsParent);
        if (parentNode is null) return;

        Assert.False(parentNode.IsExpanded);
        vm.SelectCategory(parentNode);

        Assert.True(parentNode.IsExpanded);
    }

    [Fact]
    public async Task SelectCategory_ParentNode_ClickAgainCollapses()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        var parentNode = vm.CategoryNodes.FirstOrDefault(n => n.IsParent);
        if (parentNode is null) return;

        vm.SelectCategory(parentNode);
        Assert.True(parentNode.IsExpanded);

        // deselect then re-select to simulate clicking again
        vm.SelectCategory(null);
        vm.SelectCategory(parentNode);
        Assert.False(parentNode.IsExpanded);
    }

    [Fact]
    public async Task NavigateToSubCategory_SelectsTargetNode()
    {
        var vm = CreateViewModelWithDiscoveredFields(CreateDiscoveredFieldsWithTurns());
        await vm.OpenCommand.ExecuteAsync(null);

        var parentNode = vm.CategoryNodes.FirstOrDefault(n => n.IsParent);
        if (parentNode is null) return;

        vm.SelectCategory(parentNode);
        Assert.NotEmpty(vm.SubCategorySummaries);

        var card = vm.SubCategorySummaries[0];
        vm.NavigateToSubCategoryCommand.Execute(card);

        Assert.Same(card.TargetNode, vm.SelectedCategory);
        Assert.True(vm.ShowCategoryFields);
        Assert.False(vm.ShowCategoryCards);
    }

    [Fact]
    public async Task AdvancedTab_InitialState_NoCards()
    {
        var vm = CreateViewModelWithDiscoveredFields();
        await vm.OpenCommand.ExecuteAsync(null);

        Assert.False(vm.ShowCategoryCards);
        Assert.False(vm.ShowCategoryFields);
        Assert.Empty(vm.SubCategorySummaries);
    }

    // test doubles
    private sealed class FakeSaveFileService : ISaveFileService
    {
        private readonly SaveDocument _document;
        public List<(string Path, SaveDocument Document)> SaveCalls { get; } = [];

        public FakeSaveFileService(SaveDocument document) => _document = document;

        public Task<SaveDocument> OpenAsync(string filePath) => Task.FromResult(_document);

        public Task SaveAsync(string filePath, SaveDocument document)
        {
            SaveCalls.Add((filePath, document));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingSaveFileService : ISaveFileService
    {
        private readonly SaveDocument _document;

        public FailingSaveFileService(SaveDocument document) => _document = document;

        public Task<SaveDocument> OpenAsync(string filePath) => Task.FromResult(_document);

        public Task SaveAsync(string filePath, SaveDocument document)
            => throw new IOException("Simulated save failure");
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        private readonly string? _filePath;
        public FakeFileDialogService(string? filePath) => _filePath = filePath;
        public Task<string?> OpenFileAsync() => Task.FromResult(_filePath);
    }

    private sealed class FakeFieldDiscoveryService : IFieldDiscoveryService
    {
        private readonly IReadOnlyList<FieldDefinition>? _fields;

        public FakeFieldDiscoveryService(IReadOnlyList<FieldDefinition>? fields = null)
            => _fields = fields;

        public IReadOnlyList<FieldDefinition> DiscoverFields(SaveDocument document)
            => _fields ?? [];
    }
}
