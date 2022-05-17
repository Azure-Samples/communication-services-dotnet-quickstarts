---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Email Sample

This sample application shows how the Azure Communication Services Email SDK can be used to build an email experience. This sample sends an email to the required recipients of any domain using [Email Communication Services resource](https://review.docs.microsoft.com/en-us/azure/communication-services/quickstarts/email/create-email-communication-resource?branch=pr-en-us-192537)

Additional documentation for this sample can be found on [Microsoft Docs](https://review.docs.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview?branch=pr-en-us-192537).

## Prerequisites

- Create an Azure account with an active subscription. For details, see [Create an account for free](https://azure.microsoft.com/free/)
- [Visual Studio (2019 and above)](https://visualstudio.microsoft.com/vs/)
- [.NET core 3.1](https://dotnet.microsoft.com/en-us/download/dotnet/3.1) (Make sure to install version that corresponds with your visual studio instance, 32 vs 64 bit)
- Create an Azure Communication Services resource. For details, see [Create an Azure Communication Resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your resource **connection string** for this sample.
- Create an [Azure Email Communication Services resource](https://review.docs.microsoft.com/en-us/azure/communication-services/quickstarts/email/create-email-communication-resource?branch=pr-en-us-192537) to start sending emails.

> Note: We can send an email from our own verified domain also [Add custom verified domains to Email Communication Service](https://review.docs.microsoft.com/en-us/azure/communication-services/quickstarts/email/add-custom-verified-domains?branch=pr-en-us-192537).

## Code structure

- ./send-email/Program.cs: Entry point into the email sample.
- ./send-email/EmailConfiguration.cs: Class to store all the configuration details and maintain the list of email templates data.
- ./send-email/SendEmail.cs: Class for sending emails and for checking there status.
- ./send-email/App.config: Configuration file.
- ./send-email/data/data_file.json: json file with list of email templates data.

## Before running the sample for the first time

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/communication-services-dotnet-quickstarts.git`.


### Locally configuring the application

1. Go to send-email folder and open `Emailsample.sln` solution in Visual Studio
2. Open the App.config file to configure the following settings
	- `Connectionstring`: Azure Communication Service resource's connection string.


3. Open `data/data_file.json` and add email data in following template:
	```
    [
      {
        "Id": "1",
        "TemplateName": "Single Reciepent with Attchment",
        "Subject": "Test Email for Single Recipient",
        "PlainText": "Test Email from Email Sample\n\n This email is part of testing of email communication service. \\n Best wishes",
        "HTMLText": "<html><head><title>ACS Email as a Service</title></head><body><h1>ACS Email as a Service - Html body</h1>
        <h2>This email is part of testing of email communication service</h2></body></html>",
        "Sender": "Test_Email_Comm@guid.azurecomm.net",
        "Recipients": "alice@contoso.com, Alice",
        "Importance": "High",
        "Attachments": "data//attachment.pdf"
      },
      {
        "Id": "2",
        "TemplateName": "Single Reciepent with multiple Attchments",
        "Subject": "Testing Email for Single Recipient with multiple Attchments",
        "PlainText": "Test Email from Email Sample\n\n This email is part of testing of email communication service. \\n Best wishes",
        "HTMLText": "<html><head><title>ACS Email as a Service</title></head><body><h1>ACS Email as a Service - Html body</h1><h2>This email is part of testing of email communication service</h2></body></html>",
        "Sender": "Test_Email_Comm@guid.azurecomm.net",
        "Recipients": "alice@contoso.com, Alice",
        "Importance": "High",
        "Attachments": "data//attachment.pdf; data//attachment.txt"
      },
      {
        "Id": "3",
        "TemplateName": "Multiple Recipients with Attachment",
        "Subject": "Testing Email for Multiple Recipients with Attachment",
        "PlainText": "Test Email from Email Sample\n\n This email is part of testing of email communication service. \\n Best wishes",
        "HTMLText": "<html><head><title>ACS Email as a Service Html title</title></head><body><h1>ACS Email as a Service - Html body</h1><h2>This email is part of testing of email communication service</h2></body></html>",
        "Sender": "Test_Email_Comm@guid.azurecomm.net",
        "Recipients": "alice@contoso.com, Alice; bob@contoso.com, Bob",
        "Importance": "High",
        "Attachments": "data//attachment.pdf"
      }
    ]
	```

4. Email template data: .
	- `Id` : Unique Id of an email template.
	- `TemplateName`: To identify the test data.
	- `Subject`: Email subject.
	- `PlainText`: Email content in plain text.
	- `HTMLText`: HTML content if any.
	- `Importance`: Importance of Email as High or Normal. The importance of email is Normal by default.
	- `Attachments`: Provide documents path to send if any.
	- `Recipients`: one or multiple recipients and their display name in following format.

	```
	Format : email-address-1, display-name-1 ; email-address-2, display-name-2
	For e.g. "email-1@gmail.com, email1-name ; email-2@microsoft.com, email2-name"
	```
	- Sender: Sender's email address get it from email communication service resource under provision domain.

5. Run `EmailSample` project.
