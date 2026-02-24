using System.Text.Json.Nodes;

namespace SuzerainSaveEditor.Core.Models;

// root in-memory representation of a suzerain save file
public sealed class SaveDocument
{
    public required SaveMetadata Metadata { get; init; }

    // stored as opaque json to preserve unknown structures in war saves
    public required JsonObject WarSaveData { get; init; }

    // preserves insertion order for round-trip fidelity
    public required IReadOnlyList<LuaVariable> Variables { get; init; }

    public required IReadOnlyList<EntityUpdate> EntityUpdates { get; init; }

    // lazy indices for O(1) lookups 
    private Dictionary<string, int>? _variableIndex;
    private Dictionary<(string NameInDatabase, string FieldName), int>? _entityIndex;

    internal Dictionary<string, int> VariableIndex =>
        _variableIndex ??= BuildVariableIndex();

    internal Dictionary<(string NameInDatabase, string FieldName), int> EntityIndex =>
        _entityIndex ??= BuildEntityIndex();

    
    internal SaveDocument ReplaceMetadata(SaveMetadata metadata)
    {
        return new SaveDocument
        {
            Metadata = metadata,
            WarSaveData = WarSaveData,
            Variables = Variables,
            EntityUpdates = EntityUpdates,
            _variableIndex = _variableIndex,
            _entityIndex = _entityIndex
        };
    }

    internal SaveDocument ReplaceVariable(int index, LuaVariable variable)
    {
        var newVariables = new List<LuaVariable>(Variables);
        newVariables[index] = variable;

        return new SaveDocument
        {
            Metadata = Metadata,
            WarSaveData = WarSaveData,
            Variables = newVariables,
            EntityUpdates = EntityUpdates,
            _variableIndex = _variableIndex,
            _entityIndex = _entityIndex
        };
    }

    internal SaveDocument ReplaceEntityUpdate(int index, EntityUpdate entityUpdate)
    {
        var newUpdates = new List<EntityUpdate>(EntityUpdates);
        newUpdates[index] = entityUpdate;

        return new SaveDocument
        {
            Metadata = Metadata,
            WarSaveData = WarSaveData,
            Variables = Variables,
            EntityUpdates = newUpdates,
            _variableIndex = _variableIndex,
            _entityIndex = _entityIndex
        };
    }

    private Dictionary<string, int> BuildVariableIndex()
    {
        var index = new Dictionary<string, int>(Variables.Count);
        for (var i = 0; i < Variables.Count; i++)
            index[Variables[i].Key] = i;
        return index;
    }

    private Dictionary<(string, string), int> BuildEntityIndex()
    {
        var index = new Dictionary<(string, string), int>(EntityUpdates.Count);
        for (var i = 0; i < EntityUpdates.Count; i++)
            index[(EntityUpdates[i].NameInDatabase, EntityUpdates[i].FieldName)] = i;
        return index;
    }
}
