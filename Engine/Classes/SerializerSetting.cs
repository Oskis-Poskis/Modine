using OpenTK.Mathematics;
using Newtonsoft.Json;

public class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Deserialize the Vector2 object as usual
        serializer.Populate(reader, existingValue);

        // Ignore the PerpendicularRight property
        return new Vector2(existingValue.X, existingValue.Y);
    }

    public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
    {
        // Write the Vector2 object as usual
        writer.WriteStartObject();
        writer.WritePropertyName("X");
        writer.WriteValue(value.X);
        writer.WritePropertyName("Y");
        writer.WriteValue(value.Y);
        writer.WriteEndObject();
    }
}

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Deserialize the Vector3 object as usual
        serializer.Populate(reader, existingValue);

        // Ignore the PerpendicularRight and PerpendicularLeft properties
        return new Vector3(existingValue.X, existingValue.Y, existingValue.Z);
    }

    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        // Write the Vector3 object as usual
        writer.WriteStartObject();
        writer.WritePropertyName("X");
        writer.WriteValue(value.X);
        writer.WritePropertyName("Y");
        writer.WriteValue(value.Y);
        writer.WritePropertyName("Z");
        writer.WriteValue(value.Z);
        writer.WriteEndObject();
    }
}
