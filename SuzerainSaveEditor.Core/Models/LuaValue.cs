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

    public abstract string ToLuaString();
}
