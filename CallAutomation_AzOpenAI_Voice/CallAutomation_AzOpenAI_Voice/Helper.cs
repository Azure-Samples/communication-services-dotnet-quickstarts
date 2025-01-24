using System.Text.Json.Nodes;

public static class Helper
{
    public static JsonObject GetJsonObject(BinaryData data)
    {
        return JsonNode.Parse(data).AsObject();
    }
    public static string GetCallerId(JsonObject jsonObject)
    {
        return (string)(jsonObject["from"]["rawId"]);
    }

    public static string GetIncomingCallContext(JsonObject jsonObject)
    {
        return (string)jsonObject["incomingCallContext"];
    }
}