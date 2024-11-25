using Azure.Communication.CallAutomation;
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

   /* public static CreateCallOptions CreateCallOptions(JsonObject jsonObject)
    {
        return new CreateCallOptions(callInvite, callbackUri)
        {
            //MediaStreamingOptions = defaultMediaStreaming,
            //TranscriptionOptions = defaultTrans,
            //CallIntelligenceOptions = callIntelligent
        };
    }*/


    public static CallIntelligenceOptions GetCallIntelligenceOptions(
        string cognitiveServiceEndpoint)
    {
        return new CallIntelligenceOptions
        {
            CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
        };
    }
}
