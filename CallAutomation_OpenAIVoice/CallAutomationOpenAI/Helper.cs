using System.Text;

using System.Text.Json.Nodes;
using Azure.Communication.CallAutomation;
using Azure.Communication.Identity;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using NAudio.Wave;

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