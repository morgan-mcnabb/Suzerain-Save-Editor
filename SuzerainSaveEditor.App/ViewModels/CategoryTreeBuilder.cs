using SuzerainSaveEditor.Core.Schema;
using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.App.ViewModels;

public static class CategoryTreeBuilder
{
    public sealed record BuildResult(
        List<CategoryNodeViewModel> RootNodes,
        Dictionary<string, CategoryNodeViewModel> NodeLookup);

    public static BuildResult Build(
        IReadOnlyList<FieldViewModel> advancedFields,
        ISchemaService schema,
        Dictionary<string, FieldViewModel> fieldLookup)
    {
        var nodeLookup = new Dictionary<string, CategoryNodeViewModel>(StringComparer.Ordinal);

        // get field definitions for advanced fields
        var advancedDefs = new List<FieldDefinition>(advancedFields.Count);
        foreach (var vm in advancedFields)
        {
            var def = schema.GetById(vm.FieldId);
            if (def is not null)
                advancedDefs.Add(def);
        }

        // build hierarchical categories
        var categories = AdvancedFieldGrouper.GroupFieldsHierarchical(advancedDefs);

        // convert FieldCategory tree to CategoryNodeViewModel tree
        var rootNodes = new List<CategoryNodeViewModel>(categories.Count);
        foreach (var category in categories)
        {
            var node = BuildNode(category, fieldLookup, parent: null);
            rootNodes.Add(node);
            IndexNode(node, nodeLookup);
        }

        return new BuildResult(rootNodes, nodeLookup);
    }

    private static CategoryNodeViewModel BuildNode(
        FieldCategory category,
        Dictionary<string, FieldViewModel> vmLookup,
        CategoryNodeViewModel? parent)
    {
        // resolve field VMs for this category's leaf fields
        var fieldVms = new List<FieldViewModel>();
        foreach (var def in category.Fields)
        {
            if (vmLookup.TryGetValue(def.Id, out var vm))
                fieldVms.Add(vm);
        }

        // build children first with null parent (fixed up below)
        var childNodes = new List<CategoryNodeViewModel>();
        foreach (var childCategory in category.Children)
        {
            var childNode = BuildNode(childCategory, vmLookup, parent: null);
            childNodes.Add(childNode);
        }

        // create node with actual children
        var node = new CategoryNodeViewModel(
            category.Key,
            category.Label,
            category.SortOrder,
            fieldVms,
            childNodes,
            parent);

        // fix up children's parent references to point to this node
        foreach (var child in childNodes)
            child.Parent = node;

        return node;
    }

    private static void IndexNode(
        CategoryNodeViewModel node,
        Dictionary<string, CategoryNodeViewModel> lookup)
    {
        lookup[node.Key] = node;
        foreach (var child in node.Children)
            IndexNode(child, lookup);
    }
}
