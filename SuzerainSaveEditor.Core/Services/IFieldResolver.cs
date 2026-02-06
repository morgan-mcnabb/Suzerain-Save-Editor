using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// reads and writes field values from a save document using schema field definitions
public interface IFieldResolver
{
    string? ReadValue(SaveDocument document, FieldDefinition field);
    SaveDocument WriteValue(SaveDocument document, FieldDefinition field, string value);
}
