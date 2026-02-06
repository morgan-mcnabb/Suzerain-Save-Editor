using System.Text;
using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Core.Parsing;

/// <summary>
/// serializes a list of lua variables back to the Variable={...}; format
/// </summary>
public static class LuaTableSerializer
{
    private const string Prefix = "Variable={";
    private const string Suffix = "}; ";

    public static string Serialize(IReadOnlyList<LuaVariable> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var sb = new StringBuilder();
        sb.Append(Prefix);

        for (var i = 0; i < variables.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");

            var variable = variables[i];
            sb.Append("[\"");
            sb.Append(variable.Key);
            sb.Append("\"]=");
            sb.Append(variable.Value.ToLuaString());
        }

        sb.Append(Suffix);
        return sb.ToString();
    }
}
