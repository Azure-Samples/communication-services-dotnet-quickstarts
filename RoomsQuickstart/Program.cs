using Azure.Communication.Rooms;
using Azure.Communication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RoomsQuickstart
{
    class Program
    {
        private static readonly string connectionString = "<ConnectionString>";
        static RoomsClient? roomsClient;
        private static readonly List<string> rooms = new List<string> { };
        static readonly string presenter = "<CommunicationIdentifier>";
        static readonly string attendee = "<CommunicationIdentifier>";
        static readonly string[] addeddParticipant = { "<CommunicationIdentifier>" };

        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Rooms Quickstart");

            try
            {
                // Find your Communication Services resource in the Azure portal
                roomsClient = new RoomsClient(connectionString);
                await CreateRoom();
                await UpdateRoom(rooms[0]);
                await UpdateParticipants(rooms[0]);
                await AddParticipants(rooms[0], addeddParticipant);
                await RemoveParticipants(rooms[0], addeddParticipant);
                await DeleteRoom(rooms[0]);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to perform rooms operations -> {ex}");
            }
        }

        static async Task CreateRoom()
        {
            try
            {
                Console.WriteLine("\n---------Create Room---------\n");
                List<RoomParticipant> roomParticipants = new List<RoomParticipant>();
                var participant1 = new RoomParticipant(new CommunicationUserIdentifier(presenter), RoleType.Presenter);
                var participant2 = new RoomParticipant(new CommunicationUserIdentifier(attendee), RoleType.Attendee);

                roomParticipants.Add(participant1);
                roomParticipants.Add(participant2);

                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                DateTimeOffset validFrom = DateTimeOffset.UtcNow;
                DateTimeOffset validUntil = DateTimeOffset.UtcNow.AddDays(1);

                if (roomsClient is not null)
                {
                    RoomModel createRoomResult = await roomsClient.CreateRoomAsync(validFrom, validUntil, RoomJoinPolicy.InviteOnly, roomParticipants, cancellationToken);
                    rooms.Add(createRoomResult.Id);

                    RoomModel roomInfo = await roomsClient.GetRoomAsync(createRoomResult.Id);
                    PrintRoom(roomInfo);
                }
                else
                {
                    throw new Exception("roomsClient is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create room, ex --> {ex}");
            }
        }

        static async Task UpdateRoom(string roomId)
        {
            try
            {
                Console.WriteLine("\n---------Update Room---------\n");
                CancellationToken cancellationToken = new CancellationTokenSource().Token;

                DateTimeOffset validFrom = DateTimeOffset.UtcNow;
                DateTimeOffset validUntil = DateTimeOffset.UtcNow.AddDays(10);

                if (roomsClient is not null)
                {
                    RoomModel updatedRoom = await roomsClient.UpdateRoomAsync(roomId, validFrom, validUntil, RoomJoinPolicy.InviteOnly, null, cancellationToken);
                    PrintRoom(updatedRoom);
                }
                else
                {
                    throw new Exception("roomsClient is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update room, id: {roomId}, ex --> {ex}");
            }
        }

        static public async Task AddParticipants(string roomId, string[] roomParticipants)
        {
            try
            {
                Console.WriteLine("\n---------Add participant in Room---------\n");
                List<RoomParticipant> participants = new List<RoomParticipant>();

                foreach (var p in roomParticipants)
                {
                    var participant = new RoomParticipant(new CommunicationUserIdentifier(p), RoleType.Attendee);
                    participants.Add(participant);
                }

                CancellationToken cancellationToken = new CancellationTokenSource().Token;

                if (roomsClient is not null)
                {
                    var addParticipantsResult = await roomsClient.AddParticipantsAsync(roomId, participants, cancellationToken);
                    Console.WriteLine("Participants after AddParticipantsAsync:");
                    await GetRoomParticipants(roomId);
                }
                else
                {
                    throw new Exception("roomsClient is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add participant in room, id: {roomId}, ex --> {ex}");
            }
        }

        static public async Task RemoveParticipants(string roomId, string[] roomParticipants)
        {
            try
            {
                Console.WriteLine("\n---------Remove participant from the Room---------\n");
                List<CommunicationUserIdentifier> participants = new List<CommunicationUserIdentifier>();

                foreach (var p in roomParticipants)
                {
                    var participant = new CommunicationUserIdentifier(p);
                    participants.Add(participant);
                }

                if (roomsClient is not null)
                {
                    CancellationToken cancellationToken = new CancellationTokenSource().Token;
                    var removeParticipantsResult = await roomsClient.RemoveParticipantsAsync(roomId, participants, cancellationToken);
                    Console.WriteLine("Participants after RemoveParticipantsAsync:");
                    await GetRoomParticipants(roomId);
                }
                else
                {
                    throw new Exception("roomsClient is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove participant from room, id: {roomId}, ex --> {ex}");
            }
        }

        static async Task UpdateParticipants(string roomId)
        {
            try
            {
                Console.WriteLine("\n---------Update participants in Room---------\n");
                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                List<RoomParticipant> participants = new List<RoomParticipant>();

                if (roomsClient is not null)
                {
                    ParticipantsCollection existingParticipants = await roomsClient.GetParticipantsAsync(roomId, cancellationToken);
                    var updateParticipant = await roomsClient.UpdateParticipantsAsync(roomId, existingParticipants.Participants);
                    Console.WriteLine($"Successfully updated participants in room with id: {roomId}");
                    await GetRoomParticipants(roomId);
                }
                else
                {
                    throw new Exception("roomsClient is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to Update participants in room with id: {roomId} ex --> {ex}");
            }
        }

        static async Task DeleteRoom(string roomId)
        {
            try
            {
                Console.WriteLine("\n---------Delete Room---------\n");
                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                if (roomsClient is not null)
                {
                    var deleteRoomResult = await roomsClient.DeleteRoomAsync(roomId, cancellationToken);
                    Console.WriteLine($"Successfully deleted room with id: {roomId}");
                }
                else
                {
                    throw new Exception("roomsClient is null");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to delete room with id: {roomId} ex --> {ex}");
            }
        }

        static void PrintRoom(RoomModel roomInfo)
        {
            Console.WriteLine($"room_id: {roomInfo.Id}");
            Console.WriteLine($"created_date_time: {roomInfo.CreatedDateTime}");
            Console.WriteLine($"valid_from: {roomInfo.ValidFrom}, valid_until: {roomInfo.ValidUntil}");
            Console.WriteLine($"{roomInfo.Participants.Count} participants: ");
            foreach (RoomParticipant participant in roomInfo.Participants)
            {
                Console.WriteLine($"-> {participant.CommunicationIdentifier.ToString()}");
            }
        }

        static async Task GetRoomParticipants(string roomId)
        {
            try
            {
                if (roomsClient is not null)
                {
                    CancellationToken cancellationToken = new CancellationTokenSource().Token;
                    ParticipantsCollection participants = await roomsClient.GetParticipantsAsync(roomId, cancellationToken);
                    foreach (RoomParticipant participant in participants.Participants)
                    {
                        Console.WriteLine(participant.CommunicationIdentifier.ToString());
                    }
                }
                else
                {
                    throw new Exception("roomsClient is null");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"failed to get participants of room, with id: {roomId} --> Exception: {ex}");
            }
        }
    }
}

