using System.Text;
using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Core.Parsing;

public static class LuaTableSerializer
{
    private const string Prefix = "Variable={";
    private const string Suffix = "}; ";

    public static string Serialize(IReadOnlyList<LuaVariable> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var sb = new StringBuilder(variables.Count * 64 + Prefix.Length + Suffix.Length);
        sb.Append(Prefix);

        for (var i = 0; i < variables.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");

            var variable = variables[i];
            sb.Append("[\"");
            AppendEscapedKey(sb, variable.Key);
            sb.Append("\"]=");
            sb.Append(variable.Value.ToLuaString());
        }

        sb.Append(Suffix);
        return sb.ToString();
    }

    private static void AppendEscapedKey(StringBuilder sb, string key)
    {
        if (!LuaEscaping.NeedsEscaping(key))
        {
            sb.Append(key);
            return;
        }

        LuaEscaping.AppendEscaped(sb, key);
    }
}
