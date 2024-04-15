---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
- azure-open-ai
- azure-function-app
---

# Job Router Instructions on how to build this code sample from scratch, look at [Quickstart: Job Router using OpenAI for matching](https://learn.microsoft.com/azure/communication-services/quickstarts/router/job-router-azure-openai-integration)

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- An active Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- An active azure open AI resource and model depolyment (model must be gpt 3.5 turbo or higher). [Create and deploy an Azure OpenAI Service resource](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).
- Visual Code with the Azure Function Extension
- The latest version [.NET client library](https://dotnet.microsoft.com/download/dotnet) for your operating system.

## Code Structure

- **./JobRouterOpenAIIntegration/JR_AOAI_Integration/Program.cs:** Core application code.
- **./JobRouterOpenAIIntegration/JR_AOAI_Integration.csproj:** Project configuration file.
- **./JobRouterOpenAIIntegration/JR_AOAI_Integration/appsettings.json:** Configuration file.
- **./JobRouterOpenAIIntegration/OpenAIScoringFunction/OpenAIScorer:** Function application code.
- **./JobRouterOpenAIIntegration.sln:** Visual Studio solution.

## Before running sample code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. Open the OpenAIScoringFunction folder in Visual Code. Deploy Function App to your Azure Subscription.
4. Navigate to your newly created function in the Azure Portal. Add the following Enviormental Variables (You will need to get the OpenAI URI, Key, and your DeploymentName from your Azure OpenAi Resource): 

| Name            | Value |
|-----------------|-------|
| OpenAIBaseUrl   | The Endpoint value on the Keys and Endpoints tab of your OpenAI resource. |
| OpenAIApiKey    | The Key 1 or Key 2 value on the Keys and Endpoints tab of your OpenAI resource. |
| DeploymentName  | The Deployment Name of the OpenAI model. |
| Preprompt       | You are helping pair a customer with an agent in a contact center. You will evaluate the best available agent based on their performance indicators below. Note that CSAT holds the average customer satisfaction score between 1 and 3, higher is better. Outcome is a score between 0 and 1, higher is better. AHT is average handling time, lower is better. If AHT provided is 00:00, please ignore it in the scoring. |
| Postprompt      | Respond with only a json object with agent Id as the key, and scores based on suitability for this customer as the value in a range of 0 to 1. Do not include any other information. |
| DefaultCSAT     | 1.5   |
| DefaultOutcome  | 0.5   |
| DefaultAHT      | 00:10:00 |


5. In appsettings.json in the JR_AOAI_Integration project, Update `<ACSConnectionString>` with Azure Communication Resource connection string and update the '<AzureFunctionUri>' and '<AzureFunctionKey>' in the OpenAiPairing pairing section.

## Run Locally

1. Open `JobRouterOpenAIIntegration.sln`
2. Run the `JR_AOAI_Integration` project