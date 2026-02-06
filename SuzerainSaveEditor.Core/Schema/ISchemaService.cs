namespace SuzerainSaveEditor.Core.Schema;

// provides access to field definitions for the save editor ui
public interface ISchemaService
{
    IReadOnlyList<FieldDefinition> GetAll();
    IReadOnlyList<FieldDefinition> GetByGroup(FieldGroup group);
    FieldDefinition? GetById(string id);
    IReadOnlyList<FieldDefinition> Search(string query);
}
