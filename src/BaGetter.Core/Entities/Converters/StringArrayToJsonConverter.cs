using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BaGetter.Core;

public class StringArrayToJsonConverter : ValueConverter<string[], string>
{
    public static readonly StringArrayToJsonConverter Instance = new StringArrayToJsonConverter();

    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions();

    public StringArrayToJsonConverter()
        : base(
            v => JsonSerializer.Serialize(v, _options),
            v => (!string.IsNullOrEmpty(v)) ? JsonSerializer.Deserialize<string[]>(v, _options) : Array.Empty<string>())
    {
    }
}
