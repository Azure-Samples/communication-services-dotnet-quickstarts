# Azure Communication Rooms client library for .NET

This package contains a C# SDK for the Rooms Service of Azure Communication Services.

[Source code][source] | [Package (NuGet)][package] | [Product documentation][product_docs]
## Getting started

### Install the package
Install the Azure Communication Rooms client library for .NET with [NuGet][nuget]:

```PowerShell
dotnet add package Azure.Communication.Rooms
``` 

### Prerequisites
You need an [Azure subscription][azure_sub] and a [Communication Service Resource][communication_resource_docs] to use this package.

To create a new Communication Service, you can use the [Azure Portal][communication_resource_create_portal], the [Azure PowerShell][communication_resource_create_power_shell], or the [.NET management client library][communication_resource_create_net].

### Key concepts
`RoomsClient` provides the functionality to create room, update room, get room, delete room, add participants, remove participant, update participant and get participants.

### Using statements
```C# Snippet:Azure_Communication_Rooms_Tests_UsingStatements
using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Communication.Rooms
```

### Authenticate the client
Rooms clients can be authenticated using the connection string acquired from an Azure Communication Resource in the [Azure Portal][azure_portal].

```C# Snippet:Azure_Communication_Rooms_Tests_Samples_CreateRoomsClient
var connectionString = "<connection_string>"; // Find your Communication Services resource in the Azure portal
RoomsClient client = new RoomsClient(connectionString);
```

## Examples

The following sections provide several code snippets covering some of the most common tasks, which is available at Sample1_RoomsRequesets.md

## Troubleshooting
### Service Responses
A `RequestFailedException` is thrown as a service response for any unsuccessful requests. The exception contains information about what response code was returned from the service.
```C# Snippet:Azure_Communication_RoomsClient_Tests_Troubleshooting
try
{
    CommunicationIdentityClient communicationIdentityClient = CreateInstrumentedCommunicationIdentityClient();
    var communicationUser1 = communicationIdentityClient.CreateUserAsync().Result.Value.Id;
    var communicationUser2 = communicationIdentityClient.CreateUserAsync().Result.Value.Id;
    var validFrom = DateTime.UtcNow;
    var validUntil = validFrom.AddDays(1);
    List<RoomParticipant> createRoomParticipants = new List<RoomParticipant>();
    RoomParticipant participant1 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser1), RoleType.Presenter);
    RoomParticipant participant2 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser2), RoleType.Attendee);
    Response<RoomModel> createRoomResponse = await roomsClient.CreateRoomAsync(validFrom, validUntil, RoomJoinPolicy.InviteOnly, createRoomParticipants);
    RoomModel createRoomResult = createRoomResponse.Value;
}
catch (RequestFailedException ex)
{
    Console.WriteLine(ex.Message);
}
```
