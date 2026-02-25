namespace SuzerainSaveEditor.Core.Schema;

// pre-computes lowercased search text per field so Search() avoids
// repeated case-insensitive comparisons at query time
internal static class FieldSearchIndex
{
    internal static Dictionary<string, string> Build(IReadOnlyList<FieldDefinition> fields)
    {
        var index = new Dictionary<string, string>(fields.Count, StringComparer.Ordinal);
        foreach (var f in fields)
        {
            // \0 separator prevents false cross-field substring matches
            index[f.Id] = string.Concat(
                f.Label.ToLowerInvariant(),
                "\0",
                f.Id.ToLowerInvariant(),
                "\0",
                (f.Description ?? "").ToLowerInvariant());
        }
        return index;
    }

    internal static List<FieldDefinition> Search(
        IReadOnlyList<FieldDefinition> fields,
        Dictionary<string, string> index,
        string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        var results = new List<FieldDefinition>();
        foreach (var f in fields)
        {
            if (index[f.Id].Contains(lowerQuery, StringComparison.Ordinal))
                results.Add(f);
        }
        return results;
    }
}
