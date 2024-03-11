// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAIScoringFunction
{
    public class ObjectDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        private const int MaxDepth = 8;

        public override Dictionary<string, object>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var doc = JsonDocument.ParseValue(ref reader);
            var objenum = doc.RootElement.EnumerateObject();
            var result = new Dictionary<string, object>();
            while (objenum.MoveNext())
            {
                result.Add(objenum.Current.Name, ConvertJsonElement(objenum.Current.Value, 0));
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static object? ConvertJsonElement(JsonElement element, int depth)
        {
            if (depth > MaxDepth)
                throw new JsonException("Max recursion depth exeeded.");

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var objenum = element.EnumerateObject();
                    var obj = new Dictionary<string, object>();
                    while (objenum.MoveNext())
                    {
                        obj.Add(objenum.Current.Name, ConvertJsonElement(objenum.Current.Value, depth + 1));
                    }

                    return obj;
                case JsonValueKind.Array:
                    var arrenum = element.EnumerateArray();
                    var arr = new List<object>();
                    while (arrenum.MoveNext())
                    {
                        arr.Add(ConvertJsonElement(arrenum.Current, depth + 1));
                    }

                    return arr;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.GetDecimal();
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.True:
                    return true;
            }

            return null;
        }
    }
}
