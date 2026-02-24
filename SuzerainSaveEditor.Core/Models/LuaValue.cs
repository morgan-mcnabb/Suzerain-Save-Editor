namespace SuzerainSaveEditor.Core.Models;

// discriminated union for lua variable values
public abstract record LuaValue
{
    public sealed record Bool(bool Value) : LuaValue
    {
        public override string ToLuaString() => Value ? "true" : "false";
    }

    public sealed record Int(int Value) : LuaValue
    {
        public override string ToLuaString() => Value.ToString();
    }

    public sealed record Str(string Value) : LuaValue
    {
        public override string ToLuaString()
        {
            // fast path: no special characters to escape
            if (!NeedsEscaping(Value))
                return $"\"{Value}\"";

            var sb = new System.Text.StringBuilder(Value.Length + 8);
            sb.Append('"');
            foreach (var c in Value)
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
            sb.Append('"');
            return sb.ToString();
        }

        private static bool NeedsEscaping(string value)
        {
            foreach (var c in value)
            {
                if (c is '\\' or '"' or '\n' or '\r' or '\t' or '\0')
                    return true;
            }
            return false;
        }
    }

    // scientific notation numbers (e.g. -1E+09) — preserves raw format for round-trip
    public sealed record Num(string Raw) : LuaValue
    {
        public double NumericValue => double.Parse(Raw, System.Globalization.CultureInfo.InvariantCulture);
        public override string ToLuaString() => Raw;
    }

    public abstract string ToLuaString();
}
