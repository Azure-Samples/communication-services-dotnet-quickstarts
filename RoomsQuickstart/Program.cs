using Azure.Communication.Rooms;
using Azure.Communication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Identity;

namespace RoomsQuickstart
{
    class Program
    {
        private static readonly string connectionString = "<ConnectionString>";
        static RoomsClient? roomsCollection = null;
        public static RoomsClient RoomCollection { 
            get { if (roomsCollection is not null)
                    return roomsCollection;
                else
                    return new RoomsClient(connectionString);
            } 
            set { roomsCollection = value; } 
        }

        static CommunicationIdentityClient? identityClient = null;
        public static CommunicationIdentityClient IdentityClient
        {
            get
            {
                if (identityClient is not null)
                    return identityClient;
                else
                    return new CommunicationIdentityClient(connectionString);
            }
            set { identityClient = value; }
        }

        private static readonly List<string> rooms = new List<string> { };
        static readonly CommunicationUserIdentifier user1 = IdentityClient.CreateUser();
        static readonly CommunicationUserIdentifier user2 = IdentityClient.CreateUser();
        static readonly CommunicationUserIdentifier user3 = IdentityClient.CreateUser();

        static readonly string presenter = user1.RawId;
        static readonly string attendee = user2.RawId;
        static readonly string[] addedParticipant = { user3.RawId, presenter };
        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Rooms Quickstart");

            try
            {
                // Find your Communication Services resource in the Azure portal
                if(RoomCollection is not null)
                {
                    await CreateRoom();
                    await UpdateRoom(rooms[0]);
                    await GetRoom(rooms[0]);
                    await ListRoom();
                    await AddOrUpdateParticipants(rooms[0], addedParticipant);
                    await RemoveParticipants(rooms[0], addedParticipant);
                    await DeleteRoom(rooms[0]);
                }
                else
                {
                    throw new Exception("RoomCollection is null");
                }
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
                var participant1 = new RoomParticipant(new CommunicationUserIdentifier(presenter))
                {
                    Role = ParticipantRole.Consumer
                };
                var participant2 = new RoomParticipant(new CommunicationUserIdentifier(attendee))
                {
                    Role = ParticipantRole.Attendee
                };

                roomParticipants.Add(participant1);
                roomParticipants.Add(participant2);

                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                DateTimeOffset validFrom = DateTimeOffset.UtcNow;
                DateTimeOffset validUntil = DateTimeOffset.UtcNow.AddDays(10);

                CommunicationRoom createdRoom = await RoomCollection.CreateRoomAsync(validFrom, validUntil, roomParticipants, cancellationToken);
                rooms.Add(createdRoom.Id);
                PrintRoom(createdRoom);
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
                CommunicationRoom updatedRoom = await RoomCollection.UpdateRoomAsync(roomId, validFrom, validUntil, cancellationToken);
                PrintRoom(updatedRoom);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update room, id: {roomId}, ex --> {ex}");
            }
        }

        static async Task GetRoom(string roomId)
        {
            try
            {
                Console.WriteLine("\n---------Get Room---------\n");
                CancellationToken cancellationToken = new CancellationTokenSource().Token;

                CommunicationRoom updatedRoom = await RoomCollection.GetRoomAsync(roomId, cancellationToken);
                PrintRoom(updatedRoom);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get room, id: {roomId}, ex --> {ex}");
            }
        }

        static async Task ListRoom()
        {
            try
            {
                Console.WriteLine("\n---------List Rooms---------\n");
                CancellationToken cancellationToken = new CancellationTokenSource().Token;

                AsyncPageable<CommunicationRoom> allRooms = RoomCollection.GetRoomsAsync(cancellationToken);
                await foreach (CommunicationRoom room in allRooms)
                {
                    if (room is not null)
                    {
                        Console.WriteLine("\n---------Printing first room in list rooms---------\n");
                        PrintRoom(room);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get all rooms, ex --> {ex}");
            }
        }

        static public async Task AddOrUpdateParticipants(string roomId, string[]?roomParticipants = null)
        {
            try
            {
                roomParticipants ??= new string[0];

                Console.WriteLine("\n---------Add participant in Room---------\n");
                List<RoomParticipant> participants = new List<RoomParticipant>();

                foreach (var p in roomParticipants)
                {
                    participants.Add(new RoomParticipant(new CommunicationUserIdentifier(p)) { Role = ParticipantRole.Attendee });
                }

                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                var addParticipantsResult = await RoomCollection.AddOrUpdateParticipantsAsync(roomId, participants, cancellationToken);
                Console.WriteLine("Participants after AddParticipantsAsync:");
                await GetRoomParticipants(roomId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add participant in room, id: {roomId}, ex --> {ex}");
            }
        }

        static public async Task RemoveParticipants(string roomId, string[]?roomParticipants = null)
        {
            try
            {
                roomParticipants ??= new string[0];
                Console.WriteLine("\n---------Remove participant from the Room---------\n");
                var participants = new List<CommunicationIdentifier>();

                foreach (var p in roomParticipants)
                {
                    participants.Add(new CommunicationUserIdentifier(p));
                }

                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                var removeParticipantsResult = await RoomCollection.RemoveParticipantsAsync(roomId, participants, cancellationToken);
                Console.WriteLine("Participants after RemoveParticipantsAsync:");
                await GetRoomParticipants(roomId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove participant from room, id: {roomId}, ex --> {ex}");
            }
        }

        static async Task DeleteRoom(string roomId)
        {
            try
            {
                Console.WriteLine("\n---------Delete Room---------\n");
                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                var deleteRoomResult = await RoomCollection.DeleteRoomAsync(roomId, cancellationToken);
                Console.WriteLine($"Successfully deleted room with id: {roomId}");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to delete room with id: {roomId} ex --> {ex}");
            }
        }

        static void PrintRoom(CommunicationRoom roomInfo)
        {
            Console.WriteLine($"room_id: {roomInfo.Id}");
            Console.WriteLine($"created_date_time: {roomInfo.CreatedAt}");
            Console.WriteLine($"valid_from: {roomInfo.ValidFrom}, valid_until: {roomInfo.ValidUntil}");
        }

        static async Task GetRoomParticipants(string roomId)
        {
            try
            {
                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                AsyncPageable<RoomParticipant> participants = RoomCollection.GetParticipantsAsync(roomId, cancellationToken);
                await foreach (RoomParticipant participant in participants)
                {
                    Console.WriteLine($"{participant.CommunicationIdentifier.ToString()},  {participant.Role.ToString()}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"failed to get participants of room, with id: {roomId} --> Exception: {ex}");
            }
        }
    }
}

