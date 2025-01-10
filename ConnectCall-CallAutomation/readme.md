
# Project Title

A brief description of what this project does and who it's for

|page_type|languages|products
|---|---|---|
|sample|<table><tr><td>csharp</tr></td></table>|<table><tr><td>azure</td><td>azure-communication-services</td></tr></table>|

# Connect Call - Quick Start Sample

This sample application shows how the Azure Communication Services  - Call Automation SDK can be used to connect the call. 

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You will need to record your resource **connection string** for this sample.
- Get a phone number for your new Azure Communication Services resource. For details, see [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=programming-language-csharp)
- Create Azure AI Multi Service resource. For details, see [Create an Azure AI Multi service](https://learn.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account).
- [.NET7 Framework](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you would like to clone the sample to.
2. git clone `https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`.
3. change the branch `main` to `v-pivamshi/feature/connectCall-callAutomation`.
4. Navigate to `ConnectCall-CallAutomation` folder and open `ConnectCall-CallAutomation.sln` file.

### Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 8080
devtunnel host
```


### Configuring application

Open the appsettings.json file to configure the following settings

1. `CallbackUriHost`:  Base url of the app. (For local development replace the above dev tunnel url from the above for the port 8080).
1. `CognitiveServiceEndpoint`: Azure Multi Service endpoint.
1. `AcsConnectionString`: Azure Communication Service resource's connection string.
2. `AcsPhoneNumber`: Phone number associated with the Azure Communication Service resource. For e.g. "+1425XXXAAAA"

### Run app locally

1. Run the `ConnectCall-CallAutomation` project with `dotnet run`.
2. Open `http://localhost:7051/swagger/index.html` or your dev tunnel url for the port 7051 in browser.
3. Click on createRoom from the swagger.

Once that's completed you should have a running application. The best way to test this is to place a call to your ACS phone number and talk to your intelligent agent.