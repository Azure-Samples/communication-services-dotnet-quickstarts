using System.Text;

using System.Text.Json.Nodes;
using Azure.Communication.Identity;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

public static class Helper
{
    public static JsonObject GetJsonObject(BinaryData data)
    {
        return JsonNode.Parse(data).AsObject();
    }
    public static async Task<string> ProvisionAcsIdentity(string connectionString)
    {
        var client = new CommunicationIdentityClient(connectionString);
        var user = await client.CreateUserAsync().ConfigureAwait(false);
        return user.Value.Id;
    }
    public static string GetCallerId(JsonObject jsonObject)
    {
        return (string)(jsonObject["from"]["rawId"]);
    }

    public static string GetIncomingCallContext(JsonObject jsonObject)
    {
        return (string)jsonObject["incomingCallContext"];
    }

    public static Task StartAppDevTunnel(WebApplication app)
    {

        //Get the localhost URI from Kestrel
        var server = app.Services.GetService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>();

        Uri httpsUri = null;
        foreach (var address in addressFeature.Addresses)
        {
            if (new Uri(address).Scheme == "https")
            {
                httpsUri = new Uri(address);
                break;
            }
        }

        //Initialize DevTunnel
        //https://docs.tunnels.api.visualstudio.com/cli

        return Cli.Wrap("devtunnel")
              .WithArguments(args => args
                  .Add("host")
                  .Add($"-n")
                  .Add($"cademo")
                  .Add($"-p")
                  .Add($"{httpsUri.Port}")
                  .Add($"--allow-anonymous")
                  .Add($"--protocol")
                  .Add($"https"))
                  .WithWorkingDirectory("/Users/anuj/bin")
           .WithStandardOutputPipe(PipeTarget.ToDelegate(s => Console.WriteLine(s)))
           .WithStandardErrorPipe(PipeTarget.ToDelegate(s => Console.WriteLine(s)))
           .WithValidation(CommandResultValidation.None).ExecuteBufferedAsync();
    }


}