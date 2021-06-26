---
page_type: sample
languages:
- C#
products:
- azure
- azure-communication-services
---

# Recording APIs Sample

This is a sample application to show how the Azure Communication Services server calling SDK can be used to build a call recording feature.

It's a C# based application powered by Dot net core to connect this application with Azure Communication Services.

A separate branch with end to end implementation is [available](https://github.com/Azure-Samples/communication-services-web-calling-hero/tree/public-preview). It's a public preview branch and uses beta SDKs that are not meant for production use. Please use the main branch sample for any production scenarios.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F)
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) (Make sure to install the version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this quickstart.
- An Azure storage account and container, for details, see [Create a storage account](https://docs.microsoft.com/azure/storage/common/storage-account-create?tabs=azure-portal). You'll need to record your storage **connection string** and **container name** for this quickstart.
- Create a webhook and subscribe to the recording events. For details, see [Create webhook](https://docs.microsoft.com/azure/communication-services/quickstarts/voice-video-calling/download-recording-file-sample)

## Code structure

- ./ServerRecording/Controllers : Server app core logic for calling recording APIs using Azure Communication Services server calling SDK
- ./ServerRecording/Program.cs : Entry point for the server app program logic
- ./ServerRecording/Startup.cs : Entry point for the server app startup logic

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git.
3. Once you get the config keys add the keys to the **Calling/appsetting.json**  file found under the Calling folder.
	- Input your ACS connection string in the variable: `ACSResourceConnectionString`
	- Input your storage connection string in the variable: `BlobStorageConnectionString`
	- Input blob container name for recorded media in the variable `BlobContainerName`
	- Input recording callback url for start recording api in the variable `CallbackUri`

## Locally deploying the sample app

1. Go to Calling folder and open `RecordingApi.csproj` solution in Visual Studio
2. Run `RecordingApi` project.
3. Use postman or any debugging tool and open url - https://localhost:5001

### Troubleshooting

1. Solution doesn\'t build, it throws errors during build

	Clean/rebuild the C# solution

## Publish to Azure

1. Right click the `RecordingApi` project and select Publish.
2. Create a new publish profile and select your app name, Azure subscription, resource group and etc.
3. Before publishing, add your connection string with `Edit App Service Settings`, and fill in `ResourceConnectionString` as key and connection string (copy from appsettings.json) as value
4. Detailed instructions on publishing the app to Azure are available at [Publish a Web app](https://docs.microsoft.com/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019).

**Note**: While you may use http://localhost for local testing, the sample when deployed will only work when served over https. The SDK [does not support http](https://docs.microsoft.com/azure/communication-services/concepts/voice-video-calling/calling-sdk-features#user-webrtc-over-https).

## Additional Reading

- [Azure Communication Calling SDK](https://docs.microsoft.com/azure/communication-services/concepts/voice-video-calling/calling-sdk-features) - To learn more about the calling web sdk
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core/introduction-to-aspnet-core?view=aspnetcore-3.1) - Framework for building web applications