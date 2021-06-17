---
page_type: sample
languages:
- C#
products:
- azure
- azure-communication-services
---

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fcommunication-services-web-calling-hero%2Fmain%2Fdeploy%2Fazuredeploy.json)

# Recording APIs Sample

This is a sample application to show how the Azure Communication Services server calling SDK can be used to build a call recording feature.

Its a C# based application powered by Dot net core to connect this application with Azure Communication Services.

A separate branch with end to end implementation is [available](https://github.com/Azure-Samples/communication-services-web-calling-hero/blob/teams-interop/README.md). Teams Interop is in public preview and uses beta SDKs that are not meant for production use. Please use the main branch sample for any production scenarios.

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F)
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this quickstart.

## Code structure

- ./Calling/Controllers : Server app core logic for calling recording APIs using Azure Communication Services server calling SDK
- ./Calling/Program.cs : Entry point for the server app program logic
- ./Calling/Startup.cs : Entry point for the server app startup logic

## Before running the sample for the first time
1. Get the `Connection String` from the Azure portal. For more information on connection strings, see [Create an Azure Communication Resources](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource)
2. Once you get the config keys add the keys to the **Calling/appsetting.json**  file found under the Calling folder.
	- Input your connection string in the variable: `Connectionstring`
	- Input your blob storage connectionString string in the variable: `BlobStorageConnectionString`
	- Input blob container name for recorded media in the variable `ContainerName`
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
3. Before publish, add your connection string with `Edit App Service Settings`, and fill in `ResourceConnectionString` as key and connection string (copy from appsettings.json) as value


**Note**: While you may use http://localhost for local testing, the sample when deployed will only work when served over https. The SDK [does not support http](https://docs.microsoft.com/en-us/azure/communication-services/concepts/voice-video-calling/calling-sdk-features#user-webrtc-over-https).

## Additional Reading

- [Azure Communication Calling SDK](https://docs.microsoft.com/azure/communication-services/concepts/voice-video-calling/calling-sdk-features) - To learn more about the calling web sdk
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core?view=aspnetcore-3.1) - Framework for building web applications