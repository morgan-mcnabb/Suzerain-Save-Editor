using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Core.Parsing;

/// <summary>
/// parses and serializes suzerain json save files with byte-perfect round-trip fidelity
/// </summary>
public sealed class JsonSaveParser : ISaveParser
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
        IndentSize = 4,
        IndentCharacter = ' ',
        NewLine = "\r\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public SaveDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        JsonObject root;
        try
        {
            root = JsonNode.Parse(text)?.AsObject()
                ?? throw new SaveParseException("JSON root is null.");
        }
        catch (SaveParseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SaveParseException("Invalid JSON format.", ex);
        }

        var metadata = ExtractMetadata(root);
        var warSaveData = ExtractWarSaveData(root);
        var variables = ExtractVariables(root);
        var entityUpdates = ExtractEntityUpdates(root);

        return new SaveDocument
        {
            Metadata = metadata,
            WarSaveData = warSaveData,
            Variables = variables,
            EntityUpdates = entityUpdates
        };
    }

    public string Serialize(SaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = new JsonObject
        {
            ["saveFileType"] = document.Metadata.SaveFileType,
            ["campaignName"] = document.Metadata.CampaignName,
            ["currentStoryPack"] = document.Metadata.CurrentStoryPack,
            ["turnNo"] = document.Metadata.TurnNo,
            ["saveFileName"] = document.Metadata.SaveFileName,
            ["sceneBuildIndex"] = document.Metadata.SceneBuildIndex,
            ["lastModified"] = document.Metadata.LastModified,
            ["version"] = document.Metadata.Version,
            ["isVersionMismatched"] = document.Metadata.IsVersionMismatched,
            ["isTorporModeOn"] = document.Metadata.IsTorporModeOn,
            ["notes"] = document.Metadata.Notes,
            ["warSaveData"] = document.WarSaveData.DeepClone(),
            ["variables"] = LuaTableSerializer.Serialize(document.Variables),
            ["entityUpdates"] = BuildEntityUpdatesArray(document.EntityUpdates)
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, WriterOptions);
        root.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static SaveMetadata ExtractMetadata(JsonObject root)
    {
        try
        {
            return new SaveMetadata(
                SaveFileType: GetRequired<int>(root, "saveFileType"),
                CampaignName: GetRequired<string>(root, "campaignName"),
                CurrentStoryPack: GetRequired<string>(root, "currentStoryPack"),
                TurnNo: GetRequired<int>(root, "turnNo"),
                SaveFileName: GetRequired<string>(root, "saveFileName"),
                SceneBuildIndex: GetRequired<int>(root, "sceneBuildIndex"),
                LastModified: GetRequired<string>(root, "lastModified"),
                Version: GetRequired<string>(root, "version"),
                IsVersionMismatched: GetRequired<bool>(root, "isVersionMismatched"),
                IsTorporModeOn: GetRequired<bool>(root, "isTorporModeOn"),
                Notes: GetRequired<string>(root, "notes"));
        }
        catch (Exception ex) when (ex is not SaveParseException)
        {
            throw new SaveParseException("Failed to extract metadata fields.", ex);
        }
    }

    private static JsonObject ExtractWarSaveData(JsonObject root)
    {
        var node = root["warSaveData"]
            ?? throw new SaveParseException("Missing 'warSaveData' field.");

        try
        {
            return node.DeepClone().AsObject();
        }
        catch (InvalidOperationException ex)
        {
            throw new SaveParseException("'warSaveData' is not a JSON object.", ex);
        }
    }

    private static IReadOnlyList<LuaVariable> ExtractVariables(JsonObject root)
    {
        var node = root["variables"]
            ?? throw new SaveParseException("Missing 'variables' field.");

        string variablesString;
        try
        {
            variablesString = node.GetValue<string>();
        }
        catch (Exception ex)
        {
            throw new SaveParseException("'variables' is not a string.", ex);
        }

        try
        {
            return LuaTableParser.Parse(variablesString);
        }
        catch (FormatException ex)
        {
            throw new SaveParseException("Failed to parse Lua variables table.", ex);
        }
    }

    private static IReadOnlyList<EntityUpdate> ExtractEntityUpdates(JsonObject root)
    {
        var node = root["entityUpdates"]
            ?? throw new SaveParseException("Missing 'entityUpdates' field.");

        JsonArray array;
        try
        {
            array = node.AsArray();
        }
        catch (InvalidOperationException ex)
        {
            throw new SaveParseException("'entityUpdates' is not a JSON array.", ex);
        }

        var updates = new List<EntityUpdate>(array.Count);

        for (var i = 0; i < array.Count; i++)
        {
            try
            {
                var entry = array[i]?.AsObject()
                    ?? throw new SaveParseException($"entityUpdates[{i}] is null.");
                updates.Add(new EntityUpdate(
                    NameInDatabase: entry["nameInDatabase"]!.GetValue<string>(),
                    FieldName: entry["fieldName"]!.GetValue<string>(),
                    FieldValue: entry["fieldValue"]!.GetValue<string>()));
            }
            catch (Exception ex) when (ex is not SaveParseException)
            {
                throw new SaveParseException($"Failed to parse entityUpdates[{i}].", ex);
            }
        }

        return updates;
    }

    private static JsonArray BuildEntityUpdatesArray(IReadOnlyList<EntityUpdate> entityUpdates)
    {
        var array = new JsonArray();
        foreach (var update in entityUpdates)
        {
            array.Add(new JsonObject
            {
                ["nameInDatabase"] = update.NameInDatabase,
                ["fieldName"] = update.FieldName,
                ["fieldValue"] = update.FieldValue
            });
        }
        return array;
    }

    private static T GetRequired<T>(JsonObject root, string key)
    {
        var node = root[key]
            ?? throw new SaveParseException($"Missing required field '{key}'.");

        return node.GetValue<T>();
    }
}
