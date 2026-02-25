using System.Text;

namespace SuzerainSaveEditor.Core.Parsing;

internal static class LuaEscaping
{
    internal static bool NeedsEscaping(string value)
    {
        foreach (var c in value)
        {
            if (c is '\\' or '"' or '\n' or '\r' or '\t' or '\0')
                return true;
        }
        return false;
    }

    internal static void AppendEscaped(StringBuilder sb, string value)
    {
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\0': sb.Append("\\0"); break;
                default: sb.Append(c); break;
            }
        }
    }
}
