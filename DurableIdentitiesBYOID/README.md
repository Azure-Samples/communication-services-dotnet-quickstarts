# DurableIdentitiesBYOID

This sample demonstrates how to use Azure Communication Services (ACS) to create and manage durable identities with the Bring Your Own Identity (BYOID) feature in .NET.

## Overview

The sample shows how to:
- Authenticate a `CommunicationIdentityClient` using a connection string from an environment variable.
- Create a new ACS identity.
- Use a custom ID (BYOID) to create and retrieve durable identities.
- Validate that the same custom ID always returns the same ACS identity.

## Prerequisites
- [.NET 6.0 SDK or later](https://dotnet.microsoft.com/download)
- An [Azure Communication Services resource](https://learn.microsoft.com/azure/communication-services/quickstarts/create-communication-resource)
- The `COMMUNICATION_SERVICES_CONNECTION_STRING` environment variable set with your ACS connection string.

## How to Run

1. **Clone the repository** (if you haven't already):
   ```sh
   git clone <this-repo-url>
   cd communication-services-dotnet-quickstarts/DurableIdentitiesBYOID
   ```

2. **Set the environment variable**:
   - On Windows (PowerShell):
     ```powershell
     $env:COMMUNICATION_SERVICES_CONNECTION_STRING="<your-acs-connection-string>"
     ```
   - On Linux/macOS:
     ```sh
     export COMMUNICATION_SERVICES_CONNECTION_STRING="<your-acs-connection-string>"
     ```

3. **Run the sample**:
   ```sh
   dotnet run
   ```

## What the Sample Does
- Creates a new ACS identity.
- Demonstrates BYOID by creating a user with a custom ID and retrieving its details.
- Validates that using the same custom ID returns the same identity.

## Resources
- [Azure Communication Services documentation](https://learn.microsoft.com/azure/communication-services/)
- [Azure.Communication.Identity SDK](https://learn.microsoft.com/dotnet/api/azure.communication.identity)

## License
See [LICENSE.md](../LICENSE.md) for license information.
