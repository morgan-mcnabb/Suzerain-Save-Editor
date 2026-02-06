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
}
