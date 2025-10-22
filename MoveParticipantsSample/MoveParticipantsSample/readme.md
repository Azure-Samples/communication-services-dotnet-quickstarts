| page_type | languages                               | products                                                                    |
| --------- | --------------------------------------- | --------------------------------------------------------------------------- |
| Sample    | <table><tr><td>DotNet</td></tr></table> | <table><tr><td>azure</td><td>azure-communication-services</td></tr></table> |

# Call Automation - Quick Start Sample

This sample demonstrates how to utilize the Call Automation SDK to implement a Move Participants Call scenario.

# Design

![Move Participant](./Resources/Move_Participant_Sample.jpg)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- A deployed Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- A [phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number) in your Azure Communication Services resource that can make outbound calls. NB: phone numbers are not available in free subscriptions.
- Create and host a Azure Dev Tunnel. Instructions [here](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started)
- [Python](https://www.python.org/downloads/) 3.7 or above.

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you would like to clone the sample to.
2. git clone `https://github.com/Azure-Samples/communication-services-dotnet-quickstarts.git`.
3. Navigate to `MoveParticipantsSample` folder and open `program.cs` file.

### Restore .NET packages

Restore dotnet packages by running the following command at the path \MoveParticipantsSample\MoveParticipantsSample:

```
dotnet restore
```

### Setup and host your Azure DevTunnel

[Azure DevTunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) is an Azure service that enables you to share local web services hosted on the internet. Use the commands below to connect your local development environment to the public internet. This creates a tunnel with a persistent endpoint URL and which allows anonymous access. We will then use this endpoint to notify your application of calling events from the ACS Call Automation service.

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 8080
devtunnel host
```

### Configuring application

Open `appSettings.json` file to configure the following settings

1. `acsConnectionString`: Azure Communication Service resource's connection string.
2. `callbackUriHost`: Base url of the app. (For local development use dev tunnel url)
3. `acsOutboundPhoneNumber`: ACS outbound Phone Number
4. `acsInboundPhoneNumber`: ACS Inbound Phone Number
5. `acsUserPhoneNumber`: ACS Phone Number to make the first call, external user number in real time
6. `acsTestIdentity2`: ACS identity generatd using web client
7. `acsTestIdentity3`: ACS identity generatd using web client

## Run app locally

1. Create an event subscription for incoming call
   i. Set up Web Hook for call back
   ii. Add Filters as
   From Contains: External number, Inbound Number(ACS)
   To Not Contains: 8
   iii. Deploy the event subscription
2. Navigate to `MoveParticipantsSample` folder and run application in debug mode 
3. Run the end points in sequence
   i. Create Call 1 - User Call to Call Automation
   ii. Create Call 2 - To PSTN User First And Redirect To ACS Identity
   iii.Move participants between calls
   iv. Get participants for a specific call connection
   v. Create Call 3 - To PSTN User First And Redirect To ACS Identity
   vi. Move participants between calls
   vii.Get participants for a specific call connection

