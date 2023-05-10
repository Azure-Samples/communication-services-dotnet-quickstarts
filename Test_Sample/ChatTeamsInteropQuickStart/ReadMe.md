---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-chat
- azure-communication-calling
---


# Quickstart: Join your chat app to a Teams meeting

In this quickstart, you'll learn how to chat in a Teams meeting using the Azure Communication Services Chat SDK for .NET.


## Prerequisites 

To complete this tutorial, you’ll need the following prerequisites: 
- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).  
- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/) with Universal Windows Platform development workload.  
- A deployed Communication Services resource. [Create a Communication Services resource](../../create-communication-resource.md). 
- A Teams Meeting Link.


## Code Structure

- **./ChatTeamsInteropQuickStart/MainPage.xaml:** User interface.
- **./ChatTeamsInteropQuickStart/MainPage.xaml.cs:** Code and event handling logic to establish the connection with the teams meeting and start chatting.

## Run the code locally

You can build and run the code on Visual Studio. Please note that for solution platforms we support `x64`,`x86` and `ARM64`. 

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. Open the project ChatTeamsInteropQuickStart/ChatTeamsInteropQuickStart.csproj in Visual Studio.
4. Install the following nuget packages versions (or higher):
``` csharp
Install-Package Azure.Communication.Calling -Version 1.0.0-beta.29
Install-Package Azure.Communication.Chat -Version 1.1.0
Install-Package Azure.Communication.Common -Version 1.0.1
Install-Package Azure.Communication.Identity -Version 1.0.1

```

5. With the Communication Services resource procured in pre-requisites, add the connectionstring to the **ChatTeamsInteropQuickStart/MainPage.xaml.cs** file. 

``` csharp
//ACS resource connection string i.e = "endpoint=https://your-resource.communication.azure.net/;accesskey=your-access-key";
private const string connectionString_ = "";
```
---
<b> IMPORTANT:</b>

- Select the proper platform from the 'Solution Platforms' dropdown list in Visual Studio <b>before</b> running the code. i.e `x64`
- Make sure you have the 'Developer Mode' in Windows 10 enabled (Developer Settings)

**The next steps will not work if this is not configured properly**

---


6. Press F5 to start the project in debugging mode.
7. Paste a valid teams meeting link on the 'Teems Meeting Link' box
8. Press 'Join Teams meeting' to start chatting.




## Disclaimer
Samples are non-production code, used by customers for learning and experimentation purposes.




