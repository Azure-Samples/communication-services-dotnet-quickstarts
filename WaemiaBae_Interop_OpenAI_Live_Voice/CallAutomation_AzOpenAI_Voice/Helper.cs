using System.Text.Json.Nodes;
using CallAutomationOpenAI;

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

    public static string GetServerCallId(JsonObject jsonObject)
    {
        return (string)jsonObject["serverCallId"];
    }

    /// <summary>
    /// Decodes a base64-encoded serverCallId to a URL, extracts the encoded value
    /// after "/conv/" in the path, and decodes it as a URL-safe base64 GUID.
    /// </summary>
    public static string ExtractMediaSessionId(string serverCallId)
    {
        var url = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(serverCallId));
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/');
        int convIndex = Array.IndexOf(segments, "conv");
        if (convIndex < 0 || convIndex + 1 >= segments.Length)
            throw new ArgumentException($"Could not find '/conv/{{id}}' segment in decoded serverCallId URL: {url}");

        var convSegment = segments[convIndex + 1];
        // The conv segment may contain a suffix after '~' (e.g., "pnTddMNa9U-1MJiHBvTquA~074c..."); extract only the GUID part.
        var encodedGuid = convSegment.Split('~')[0];
        if (!encodedGuid.TryDecodeUrlString(out var guid))
            throw new ArgumentException($"Could not decode '{encodedGuid}' as a GUID from serverCallId URL: {url}");

        return guid.ToString();
    }
}