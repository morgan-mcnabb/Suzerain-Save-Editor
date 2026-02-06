using SuzerainSaveEditor.Core.Models;

namespace SuzerainSaveEditor.Core.Parsing;

/// <summary>
/// parses lua table strings of the form: Variable={["key1"]=value1, ["key2"]=value2, ...};
/// </summary>
public static class LuaTableParser
{
    private const string Prefix = "Variable={";
    private const string Suffix = "}; ";

    public static IReadOnlyList<LuaVariable> Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!input.StartsWith(Prefix, StringComparison.Ordinal))
            throw new FormatException($"Expected Lua table to start with '{Prefix}'.");

        if (!input.EndsWith(Suffix, StringComparison.Ordinal))
            throw new FormatException($"Expected Lua table to end with '{Suffix}'.");

        var body = input.AsSpan()[Prefix.Length..^Suffix.Length];

        if (body.IsEmpty)
            return [];

        var variables = new List<LuaVariable>();
        var pos = 0;

        while (pos < body.Length)
        {
            // expect ["
            if (!body[pos..].StartsWith("[\""))
                throw new FormatException($"Expected '[\"' at position {Prefix.Length + pos}.");

            pos += 2; // skip ["

            // read key until we find "]=
            var keyStart = pos;
            var keyEnd = FindKeyEnd(body, pos);
            if (keyEnd < 0)
                throw new FormatException($"Could not find closing '\"]='' for key starting at position {Prefix.Length + keyStart}.");

            var key = body[keyStart..keyEnd].ToString();
            pos = keyEnd + 3; // skip "]=

            // read value
            var (value, newPos) = ReadValue(body, pos);
            pos = newPos;
            variables.Add(new LuaVariable(key, value));

            // expect , or end of body
            if (pos < body.Length)
            {
                if (!body[pos..].StartsWith(", "))
                    throw new FormatException($"Expected ', ' separator at position {Prefix.Length + pos}.");

                pos += 2; // skip ", "
            }
        }

        return variables;
    }

    // finds the position of the closing "]= sequence after a key
    // handles keys containing == by looking for "]=  specifically
    private static int FindKeyEnd(ReadOnlySpan<char> body, int start)
    {
        var pos = start;
        while (pos < body.Length - 2)
        {
            if (body[pos] == '"' && body[pos + 1] == ']' && body[pos + 2] == '=')
                return pos;
            pos++;
        }
        return -1;
    }

    private static (LuaValue value, int newPos) ReadValue(ReadOnlySpan<char> body, int pos)
    {
        if (pos >= body.Length)
            throw new FormatException($"Unexpected end of input while reading value at position {Prefix.Length + pos}.");

        var ch = body[pos];

        // boolean true
        if (body[pos..].StartsWith("true"))
            return (new LuaValue.Bool(true), pos + 4);

        // boolean false
        if (body[pos..].StartsWith("false"))
            return (new LuaValue.Bool(false), pos + 5);

        // string value
        if (ch == '"')
        {
            pos++; // skip opening quote
            var valueStart = pos;
            // read until closing quote — no escape sequences in the data
            while (pos < body.Length && body[pos] != '"')
                pos++;

            if (pos >= body.Length)
                throw new FormatException($"Unterminated string value starting at position {Prefix.Length + valueStart - 1}.");

            var value = body[valueStart..pos].ToString();
            pos++; // skip closing quote
            return (new LuaValue.Str(value), pos);
        }

        // numeric value (possibly negative)
        if (ch == '-' || char.IsDigit(ch))
        {
            var numStart = pos;
            if (ch == '-')
                pos++;

            while (pos < body.Length && char.IsDigit(body[pos]))
                pos++;

            if (pos == numStart || (ch == '-' && pos == numStart + 1))
                throw new FormatException($"Invalid numeric value at position {Prefix.Length + numStart}.");

            var numStr = body[numStart..pos];
            if (!int.TryParse(numStr, out var intValue))
                throw new FormatException($"Could not parse integer '{numStr}' at position {Prefix.Length + numStart}.");

            return (new LuaValue.Int(intValue), pos);
        }

        throw new FormatException($"Unexpected character '{ch}' at position {Prefix.Length + pos}.");
    }
}
