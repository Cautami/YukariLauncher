using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace YukariApp.Common.JsonConverters;

public class Vector2IJsonConverter : JsonConverter<Vector2I>
{
    public override Vector2I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var x = doc.RootElement.GetProperty("x").GetInt32();
        var y = doc.RootElement.GetProperty("y").GetInt32();
        return new Vector2I(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2I value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}