---
page_type: sample
languages:
- csharp
products:
- azure
- azure-communication-services
---

# Bring your own durable identity (BYODI) with Azure Communication Services

This sample demonstrates how to use Bring your own durable identity (BYODI) with Azure Communication Services (ACS) feature in .NET.

For full instructions on Azure Communication Services identity management, look at [Quickstart: Create and manage access tokens](https://docs.microsoft.com/azure/communication-services/quickstarts/access-tokens?pivots=programming-language-csharp)

## Overview

The sample shows how to:
- Authenticate a `CommunicationIdentityClient` using a connection string from an environment variable.
- Create standard ACS identities.
- Bring your own durable identity (BYODI) with Azure Communication Services to create identities with custom IDs.
- Retrieve user details including custom ID information.
- Validate that the same custom ID always returns the same ACS identity.
- Generate access tokens for BYODI identities.
- Clean up resources properly.

**Note:** This sample uses the preview version of the Azure Communication Services Identity SDK (1.4.0-beta.1) which includes the Bring your own durable identity (BYODI) with ACS functionality.

## Prerequisites

- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F). 
- Install [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- The latest version .NET Core SDK for your operating system.
- Create an Azure Communication Services resource. For details, see [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource). You'll need to record your connection string for this quickstart.

## Code Structure

- **./DurableIdentitiesBYODI/Program.cs:** Core application code with BYODI operations implementation.
- **./DurableIdentitiesBYODI/DurableIdentitiesBYODI.csproj:** Project configuration file.

## Before Running Sample Code

1. Open an instance of PowerShell, Windows Terminal, Command Prompt or equivalent and navigate to the directory that you'd like to clone the sample to.
2. `git clone https://github.com/Azure-Samples/Communication-Services-dotnet-quickstarts.git`
3. With the Communication Services procured in prerequisites, add connection string to environment variable using below command

```
setx COMMUNICATION_SERVICES_CONNECTION_STRING <CONNECTION_STRING>
```

## Run Locally

1. Open `DurableIdentitiesBYODI.csproj`
2. Run the `DurableIdentitiesBYODI` project

Alternatively, you can run the project from the command line:

```console
dotnet run
```

## Expected Output

When you run the application successfully, you should see output similar to the following:

```console
Azure Communication Services - Durable Identities BYODI Quickstart

Created a standard identity with ID: 8:acs:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx_00000028-xxxx-xxxx-xxxx-xxxxxxxxxxxx

User ID: 8:acs:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx_00000028-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Custom ID: alice@contoso.com

User ID (second call): 8:acs:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx_00000028-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Custom ID (second call): alice@contoso.com

Validation successful: Both identities have the same ID as expected.

Issued access token with 'chat' scope:
Token expires at: 6/13/2025 5:49:30 PM +00:00

Deleted identities.
```

The key points to observe:
- A standard ACS identity is created first
- Two BYODI identities are created using the same custom ID (`alice@contoso.com`)
- Both BYODI calls return the **same identity ID**, demonstrating that custom IDs map consistently to the same ACS identity
- An access token is successfully generated for the BYODI identity
- All resources are properly cleaned up at the end

## What the Sample Does

- Creates a standard ACS identity to demonstrate regular identity creation.
- Demonstrates BYODI by creating a user with a custom ID (`alice@contoso.com`) and retrieving its details.
- Validates that using the same custom ID returns the same ACS identity on subsequent calls.
- Issues access tokens for BYODI identities with different scopes.
- Demonstrates proper resource cleanup by deleting created identities.

## Key Features Demonstrated

- **Standard Identity Creation**: Shows how to create regular ACS identities
- **Identity Creation**: Uses custom IDs to create mapped ACS identities
- **Identity Persistence**: Validates that custom IDs map consistently to the same ACS identity
- **User Detail Retrieval**: Demonstrates how to get user details including custom ID information
- **Token Generation**: Shows how to generate access tokens for identities
- **Resource Management**: Proper cleanup of created identities
