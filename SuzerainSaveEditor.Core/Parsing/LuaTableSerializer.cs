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

        var sb = new StringBuilder();
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
        // fast path: no special characters to escape (true for all known save keys)
        if (!KeyNeedsEscaping(key))
        {
            sb.Append(key);
            return;
        }

        foreach (var c in key)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                default: sb.Append(c); break;
            }
        }
    }

    private static bool KeyNeedsEscaping(string key)
    {
        foreach (var c in key)
        {
            if (c is '\\' or '"')
                return true;
        }
        return false;
    }
}
