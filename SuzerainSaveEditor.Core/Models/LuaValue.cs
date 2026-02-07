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
        public override string ToLuaString() => $"\"{Value}\"";
    }

    // scientific notation numbers (e.g. -1E+09) — preserves raw format for round-trip
    public sealed record Num(string Raw) : LuaValue
    {
        public double NumericValue => double.Parse(Raw, System.Globalization.CultureInfo.InvariantCulture);
        public override string ToLuaString() => Raw;
    }

    public abstract string ToLuaString();
}
