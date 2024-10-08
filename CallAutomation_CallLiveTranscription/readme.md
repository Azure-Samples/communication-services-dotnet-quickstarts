﻿|page_type|languages|products
|---|---|---|
|sample|<table><tr><td>csharp</tr></td></table>|<table><tr><td>azure</td><td>azure-communication-services</td></tr></table>|

# Call Live Transcription - Quick Start Sample

This sample application shows how the Azure Communication Services  - Call Automation SDK can be used generate the live transcription between PSTN calls. 
It accepts an incoming call from a phone number, performs DTMF recognition, and transfer the call to agent. You can see the live transcription in websocket during the conversation between agent and user. The application is a web-based application built on .Net7 framework.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You will need to record your resource **connection string** for this sample.
- Get a phone number for your new Azure Communication Services resource. For details, see [Get a phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=programming-language-csharp)
- Create Azure AI Multi Service resource. For details, see [Create an Azure AI Multi service](https://learn.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account).
- Install ngrok. Instructions [here](https://ngrok.com/)
- Setup websocket
- [.NET7 Framework](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you would like to clone the sample to.
2. git clone `https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`.
3. Navigate to `CallAutomation_CallLiveTanscription` folder and open `CallAutomation_CallLiveTanscription.sln` file.

### Setup and host ngrok

You can run multiple tunnels on ngrok by changing ngrok.yml file as follows:

1. Open the ngrok.yml file from a powershell using the command ngrok config edit
2. Update the ngrok.yml file as follows:
    authtoken: xxxxxxxxxxxxxxxxxxxxxxxxxx
    version: "2"
    region: us
    tunnels:
    first:
        addr: 8080
        proto: http 
        host_header: localhost:8080
    second:
        proto: http
        addr: 5001
        host_header: localhost:5001
NOTE: Make sure the "addr:" field has only the port number, not the localhost url.
3. Start all ngrok tunnels configured using the following command on a powershell - ngrok start --all
4. Once you have setup the websocket server, note down the the ngrok url on your server's port as the websocket url in this application for incoming call scenario. Just replace the https:// with wss:// and update in the appsettings.json file.

### Configuring application

Open the appsettings.json file to configure the following settings

1. `CallbackUriHost`:  Base url of the app. (For local development replace the above ngrok url from the above for the port 8080).
1. `CognitiveServiceEndpoint`: Azure Multi Service endpoint.
1. `AcsConnectionString`: Azure Communication Service resource's connection string.
2. `AcsPhoneNumber`: Phone number associated with the Azure Communication Service resource. For e.g. "+1425XXXAAAA"
3. `TransportUrl`: Ngrok url for the server port (in this example port 5001) make sure to replace https:// with wss:// and add the /ws at the end ex. wss://xxxxxx.ngrok.app/ws
3. `Locale`: Transcription locale
4. `AgentPhoneNumber`: Phone number associated to with Agent

### Run app locally

1. Run the `CallAutomation_CallLiveTranscription` project with `dotnet run`
1. Make sure your websocket server running
2. Open `http://localhost:8080/swagger/index.html` or your ngrok url for the port 8080 in browser
3. Test this application by giving a call to ACS phone number