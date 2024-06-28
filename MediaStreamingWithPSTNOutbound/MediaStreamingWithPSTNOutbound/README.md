# Incoming Call Recording

Receives the incoming call event, and answer the incoming call, starts the recording, and play text to user and allow user to record the message and then it stops the recording until user disconnect the call. Once the recorded file is available for downloading it will be downloaded to project location

## Features

This project framework provides the following features:
* It starts the websocket
* It launch the swagger
* createOutBoundCall -> create a outbound call to provided pstn number with media stream enabled
* Callconnected event callback, it added the acs user to the call
* when call disconnected, the audio stream is converted to wav file and stored to your user profile downloads folder as test.wav

## Getting Started

### Prerequisites

* An Azure account with an active subscription. For details, see [Create an account for free](https://aka.ms/Mech-Azureaccount) 
* Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource.](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-azp) You'll need to record your resource connection string for this sample.
* For local run: Install Azure Dev Tunnels CLI. For details, see [Create and host dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows)
* [.NET 7](https://dotnet.microsoft.com/download)
* [Cognitive Service ](https://learn.microsoft.com/en-us/azure/search/search-create-service-portal)
* Ngrok to host on the server https://ngrok.com/docs/getting-started/

## Setup Instructions

Before running this sample, you'll need to setup the resources above with the following configuration updates:

##### 1. Add a Managed Identity to the ACS Resource that connects to the Cognitive Services resource
Follow the instructions in this [documentation](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/azure-communication-services-azure-cognitive-services-integration).

##### 2. Add the required API Keys and endpoints
Open the appsettings.json file to configure the following settings:

    
    - `AcsConnectionString`: Azure Communication Service resource's connection string.
    - `CognitiveServiceEndpoint`: The Cognitive Services endpoint
    - `BaseUrl`:  your ngrok url wwhere it points to 8080 port
	- `TransportUrl`:  your ngrok url wwhere it points to 5001 port ex. wss://xxx.ngrok-free.app/ws
        Note: in the transport makes sure you have added /ws (we initiate the websocket if the url has /ws ) ex. wss://xxx.ngrok-free.app/ws
    - `AcsPhoneNumber`:  provide your ACS phonenumber (which should be cabable of making outound call)
    - `TargetPhoneNumber`:  target phone number with country code ex. +1xxxxxxxxxx
    - `AcsTargetUser`:  acs user starts with ex. 8:acs:

### Setup and host ngrok

You can run multiple tunnels on ngrok by changing ngrok.yml file as follows:

1. Open the ngrok.yml file from a powershell using the command ngrok config edit
2. Update the ngrok.yml file as follows:
   
   
    _authtoken: xxxxxxxxxxxxxxxxxxxxxxxxxx
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
        host_header: localhost:5001_
   
**NOTE:** Make sure the "addr:" field has only the port number, not the localhost url.
4. Start all ngrok tunnels configured using the following command on a powershell - ngrok start --all
5. Once you have setup the websocket server, note down the the ngrok url on your server's port as the websocket url in this application for incoming call scenario. Just replace the https:// with wss:// and update in the appsettings.json file.

## Running the application

Best way to test this application, call to pstn user [you can refer this web ui to make call to communication identitifer or acs phone number](https://github.com/Azure-Samples/communication-services-web-calling-tutorial/blob/main/README.md)

1. Provide the ngrok url for port 8080 in the BaseUrl
2. Provide the ngrok url for port 5001 in the TransportUrl
3. Run `dotnet run` to build and run the MediaStreamingWithPSTNOutbound tool

   

