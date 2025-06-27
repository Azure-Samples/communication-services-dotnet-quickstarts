---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Access Tokens with Custom Id with Azure Communication Services

This sample demonstrates how to use Access Tokens with Custom Id with Azure Communication Services (ACS) feature in .NET.

For full instructions on Azure Communication Services identity management, look at [Quickstart: Create and manage access tokens](https://docs.microsoft.com/azure/communication-services/quickstarts/access-tokens?pivots=programming-language-csharp)

## Overview

The sample shows how to:
- Authenticate a `CommunicationIdentityClient` using a connection string from an environment variable.
- Use Access Tokens with Custom Id with Azure Communication Services to create identities with custom IDs.
- Retrieve user details including custom ID information.
- Validate that the same custom ID always returns the same ACS identity.
- Generate access tokens for custom identities.
- Clean up resources properly.

**Note:** This sample uses the preview version of the Azure Communication Services Identity SDK (1.4.0-beta.1) which includes the Access Tokens with Custom Id with ACS functionality.

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F). 
- The latest version .NET Core SDK for your operating system.
- Create an Azure Communication Services resource. For details, see [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your connection string for this quickstart.

## Code Structure

- **./AccessTokensWithCustomIdQuickstart/Program.cs:** Core application code with custom identity operations implementation.
- **./AccessTokensWithCustomIdQuickstart/AccessTokensWithCustomIdQuickstart.csproj:** Project configuration file.

## Before Running Sample Code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. With the Communication Services procured in prerequisites, add connection string to environment variable using below command

```
setx COMMUNICATION_SERVICES_CONNECTION_STRING <CONNECTION_STRING>
```

## Run Locally

1. Open `AccessTokensWithCustomIdQuickstart.csproj`
2. Run the `AccessTokensWithCustomIdQuickstart` project

Alternatively, you can run the project from the command line:

```console
dotnet run
```

## Expected Output

When you run the application successfully, you should see output similar to the following:

```console
Azure Communication Services - Access Tokens for Identity with customId Quickstart

User ID: 8:acs:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx_00000028-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Custom ID: alice@contoso.com
Access Token: eyJhbGciOiJSUzI1NiIsImtpZCI6IkRCQTFENTczNEY1MzM4QkRENjRGNjA4NjE2QTQ5NzFCOTEwNjU5QjAiLCJ4NXQiOiIyNkhWYzA5VE9MM1dUMkNHRnFTWEc1RUdXYkEiLCJ0eXAiOiJKV1QifQ.eyJ...truncated...

User ID (second call): 8:acs:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx_00000028-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Custom ID (second call): alice@contoso.com

Validation successful: Both identities have the same ID as expected.

Cleaning up: Deleting identities...

Deleted identities.
```

The key points to observe:
- Two identities are created using the same custom ID (`alice@contoso.com`)
- Both identity calls return the **same identity ID**, demonstrating that custom IDs map consistently to the same ACS identity
- Access tokens are generated for the customId-mapped identity during the creation process
- All resources are properly cleaned up at the end

## Key Features Demonstrated

- **Identity Creation with custom ID**: Uses custom IDs to create mapped ACS identities
- **Identity Persistence**: Validates that custom IDs map consistently to the same ACS identity
- **User Detail Retrieval**: Demonstrates how to get user details including custom ID information
- **Token Generation**: Shows how to generate access tokens for identities with custom IDs

