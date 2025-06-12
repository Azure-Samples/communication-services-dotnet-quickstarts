# DurableIdentitiesBYOID

This sample demonstrates how to use Azure Communication Services (ACS) to create and manage identities in .NET. The sample prepares for future Bring Your Own Identity (BYOID) functionality and shows standard identity management practices.

## Overview

The sample shows how to:
- Authenticate a `CommunicationIdentityClient` using a connection string from an environment variable.
- Create new ACS identities.
- Generate access tokens for different communication scopes (Chat, VoIP).
- Manage multiple identities and demonstrate token lifecycle.
- Clean up resources properly.

**Note:** The Bring Your Own Identity (BYOID) feature may require preview SDK versions or specific API access. This sample demonstrates the standard identity management workflow that serves as a foundation for BYOID implementations.

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
- Creates new ACS identities.
- Demonstrates token generation with different scopes (Chat, VoIP).
- Shows how to manage multiple identities.
- Validates that different identity creation calls generate unique identities.
- Demonstrates proper resource cleanup.

## Resources
- [Azure Communication Services documentation](https://learn.microsoft.com/azure/communication-services/)
- [Azure.Communication.Identity SDK](https://learn.microsoft.com/dotnet/api/azure.communication.identity)

## License
See [LICENSE.md](../LICENSE.md) for license information.
