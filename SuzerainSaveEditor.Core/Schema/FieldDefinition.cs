using System.Text.Json.Serialization;

namespace SuzerainSaveEditor.Core.Schema;

// describes a single editable field in the save file
public sealed record FieldDefinition
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public required string Label { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required FieldGroup Group { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required FieldType Type { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required FieldSource Source { get; init; }

    public string? Description { get; init; }
    public int? Min { get; init; }
    public int? Max { get; init; }
    public IReadOnlyList<string>? Options { get; init; }
}
