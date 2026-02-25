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
        public override string ToLuaString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public sealed record Str(string Value) : LuaValue
    {
        public override string ToLuaString()
        {
            if (!Parsing.LuaEscaping.NeedsEscaping(Value))
                return $"\"{Value}\"";

            var sb = new System.Text.StringBuilder(Value.Length + 8);
            sb.Append('"');
            Parsing.LuaEscaping.AppendEscaped(sb, Value);
            sb.Append('"');
            return sb.ToString();
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
