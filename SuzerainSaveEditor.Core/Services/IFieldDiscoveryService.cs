using SuzerainSaveEditor.Core.Models;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Core.Services;

// discovers unmapped variables and entity updates in a save document
public interface IFieldDiscoveryService
{
    IReadOnlyList<FieldDefinition> DiscoverFields(SaveDocument document);
}
