---
page_type: sample
languages:
- dotnet
products:
- azure
- azure-functions
- azure-communication-services
---

# Send a shortened URL link using Azure Communication Services SMS

SMS messages are limited to 160 characters. This can pose limitations when sending links to customers and truncate the message for many reasons; the link can exceed 160 characters and/or contain query parameters for the user profile and cookie information, etc. Leverage the Azure URL shortener to help stay within the 160 character limit.

## Pre-requisites

- An active Azure subscription. [Create an account for free](https://azure.microsoft.com/free/?ref=microsoft.com&utm_source=microsoft.com&utm_medium=docs&utm_campaign=visualstudio).
- An active Azure Communication Services resource. For more information, see [Create an Azure Communication Services resource](https://learn.microsoft.com/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-net).
- An Azure Communication Services phone number. [Get a phone number](https://learn.microsoft.com/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=programming-language-csharp). You will need to [verify your phone number](https://learn.microsoft.com/azure/communication-services/quickstarts/sms/apply-for-toll-free-verification) so it can send messages with URLs.
- Deployed [AzUrlShortener](https://github.com/microsoft/AzUrlShortener). Click [Deploy to Azure](https://github.com/microsoft/AzUrlShortener/wiki/How-to-deploy-your-AzUrlShortener) button for quick deploy.
  - [*Optional*] Deploy the [Admin web app](https://github.com/microsoft/AzUrlShortener/blob/main/src/Cloud5mins.ShortenerTools.TinyBlazorAdmin/README.md) to manage and monitor links in UI.
- For this tutorial, we will be leveraging an Azure Function serve as an endpoint we can call to request SMS to be sent with a shortened URL. You could always use an existing service, different framework like express or just run this as a Node.JS console app. To follow this instructions to set up an [Azure Function for C#](https://learn.microsoft.com/azure/azure-functions/create-first-function-vs-code-csharp).

## Run locally

You will need to [verify your phone number](https://learn.microsoft.com/azure/communication-services/quickstarts/sms/apply-for-toll-free-verification) to send SMS messages with URLs. Once you have submitted your verification application, it might take a couple days for the phone number to be enabled to send URLs before it gets full verified (full verification takes 5-6 weeks). For more information on toll-free number verification, see [Apply for toll-free verification](https://learn.microsoft.com/azure/communication-services/quickstarts/sms/apply-for-toll-free-verification).

1. Ensure to have the Azure Function Extension on Visual Studio. Click into the tab on the left side menu and initialize the project
2. Open a terminal and navigate to the repository directory
3. Run `cd sms-url-shortener` to get in the same directory as the function
4. Run `dotnet restore` which will install the dependencies for the sample
5. Update the values in the code to add your Azure Communication Services connection string, phone number and the endpoint for the URL shortener deployed as a pre-requisite. The sample is configured to take this values from environment variables within the `local.settings.json` file. If the file is not automatically created, you can create it and add the following values:

```json

{
    "IsEncrypted": false,
    "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "ACS_CONNECTIONSTRING": "<ACS CONNECTION STRING>",
    "ACS_PHONE_NUMBER": "<ACS PHONE NUMBER>", // Ex. +15555555555
    "URL_SHORTENER": "<URL SHORTENER ENDPOINT>" // Ex. https://<Azure Function URL>/api/UrlCreate
    }
}

```

6. In Visual Studio Code, click into the `SendSMSUrlShortener.cs` file and press `F5`. (Alternatively run `func host start` within the functions directory)

Then using a tool like [Postman](https://www.postman.com/), you can test your function by making a `POST` request to the endpoint of your Azure Function. You will need to provide the phone number and URL as query parameters. For example, if your Azure Function is running locally, you can make a request to `http://localhost:7071/api/<FUNCTION NAME>?phoneNumber=%2B15555555555&url=https://www.microsoft.com`. You should receive a response with the shortened URL and a status of `Success`.

## Deploy to Azure

To deploy your Azure Function, you can follow [step by step instructions](https://learn.microsoft.com/azure/azure-functions/create-first-function-vs-code-csharp?pivots=programming-language-dotnet#sign-in-to-azure).

Once deployed, you can access the function through a similar method as you did when testing locally. You will need to provide the phone number and URL as query parameters. For example, if your Azure Function is deployed to Azure, you can make a request to `https://<YOUR AZURE FUNCTION NAME>.azurewebsites.net/api/<FUNCTION NAME>?phoneNumber=%2B15555555555&url=https://www.microsoft.com`. You should receive a response with the shortened URL and a status of `Success`.