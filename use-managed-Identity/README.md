---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---


# Send an SMS Message Quickstart

For full instructions on how to build this code sample from scratch, look at [Quickstart: Use managed identities](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/managed-identity?pivots=programming-language-csharp)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F). 
- An active Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- An SMS enabled telephone number. [Get a phone number](https://docs.microsoft.com/azure/communication-services/quickstarts/telephony-sms/get-phone-number).
- A setup managed identity for a development environment see [Authorize access with managed identity](https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/managed-identity-from-cli)
## Code Structure

- **./use-managed-Identity/Program.cs:** Core application code with send SMS implementation.
- **./use-managed-Identity/ManagedIdentitiesQuickstart.csproj:** Project configuration file.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. With the `endpoint ` procured in pre-requisites, add it to the **use-managed-Identity/program.cs** file. Assign your endpoint in line 17:
   ```Uri endpoint = new("https://<RESOURCENAME>.communication.azure.com/");```
4. With the SMS enabled telephone number procured in pre-requisites, add it to the **use-managed-Identity/program.cs** file. Assign your ACS telephone number and sender number in line 29:
   ```SmsSendResult result = instance.SendSms(endpoint, "<Your ACS Phone Number>", "<The Phone Number you'd like to send the SMS to.>", "Hello from Managed Identities");```

## Run Locally

1. Open `ManagedIdentitiesQuickstart.csproj`
2. Run the `ManagedIdentitiesQuickstart` project