using Azure;
using Azure.Communication;
using Azure.Communication.Rooms;

namespace RoomsQuickstart
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Azure Communication Services - Rooms Quickstart");

            // Find your Communication Services resource in the Azure portal
            string connectionString = "<connection string>";

            // Replace with existing users from your Communication Services resource
            var communicationUser1 = "<CommunicationUserIdentifier.Id>";
            var communicationUser2 = "<CommunicationUserIdentifier.Id>";
            var communicationUser3 = "<CommunicationUserIdentifier.Id>";

            RoomsClient roomsClient = new RoomsClient(connectionString);

            // Create Room
            var validFrom = DateTime.UtcNow;
            var validUntil = validFrom.AddDays(1);
            RoomJoinPolicy roomJoinPolicy = RoomJoinPolicy.InviteOnly;
            List<RoomParticipant> createRoomParticipants = new List<RoomParticipant>();

            RoomParticipant participant1 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser1), RoleType.Presenter);
            RoomParticipant participant2 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser2), RoleType.Attendee);
            RoomParticipant participant3 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser3), RoleType.Consumer);
            createRoomParticipants.Add(participant1);
            createRoomParticipants.Add(participant2);
            createRoomParticipants.Add(participant3);

            Response<RoomModel> createRoomResponse = await roomsClient.CreateRoomAsync(validFrom, validUntil, RoomJoinPolicy.InviteOnly, createRoomParticipants);
            RoomModel createCommunicationRoom = createRoomResponse.Value;
            var roomId = createCommunicationRoom.Id;

            Console.WriteLine("RoomId: " + createCommunicationRoom.Id);
            Console.WriteLine("validFrom: " + createCommunicationRoom.ValidFrom);
            Console.WriteLine("validUntil: " + createCommunicationRoom.ValidUntil);
            Console.WriteLine("roomJoinPolicy: " + createCommunicationRoom.RoomJoinPolicy);

            Console.WriteLine($"\nUpdated room participants to:\n{string.Join("\n", createCommunicationRoom.Participants)}");

            // Update Room
            var updateValidFrom = DateTime.UtcNow.AddDays(1);
            var updateValidUntil = updateValidFrom.AddDays(1);

            List<RoomParticipant> updateRoomParticipants = new List<RoomParticipant>();
            var communicationUser4 = "<CommunicationUserIdentifier.Id>";
            var communicationUser5 = "<CommunicationUserIdentifier.Id>";
            RoomParticipant updateParticipant1 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser4), RoleType.Attendee);
            RoomParticipant updateParticipant2 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser5), RoleType.Attendee);
            updateRoomParticipants.Add(updateParticipant1);
            updateRoomParticipants.Add(updateParticipant2);

            Response<RoomModel> updateRoomResponse = await roomsClient.UpdateRoomAsync(roomId, updateValidFrom, updateValidUntil, null, updateRoomParticipants);
            RoomModel updatedCommunicationRoom = updateRoomResponse.Value;


            // Get Room
            Response<RoomModel> getRoomResponse = await roomsClient.GetRoomAsync(roomId);
            RoomModel room = getRoomResponse.Value;

            // Add Room Participants
            var communicationUser6 = "<CommunicationUserIdentifier.Id>";
            var communicationUser7 = "<CommunicationUserIdentifier.Id>";
            List<RoomParticipant> addRoomParticipants = new List<RoomParticipant>();
            RoomParticipant addParticipant1 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser6), RoleType.Attendee);
            RoomParticipant addParticipant2 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser7), RoleType.Attendee);
            addRoomParticipants.Add(addParticipant1);
            addRoomParticipants.Add(addParticipant2);
            Response<ParticipantsCollection> addParticipantsResponse = await roomsClient.AddParticipantsAsync(roomId, addRoomParticipants);
            ParticipantsCollection roomParticipantsAfterAddingParticipants = addParticipantsResponse.Value;

            // Update Room Participants
            List<RoomParticipant> UpdateRoomParticipants = new List<RoomParticipant>();

            RoomParticipant updateRoomParticipant1 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser6), RoleType.Presenter);
            RoomParticipant updateRoomParticipant2 = new RoomParticipant(new CommunicationUserIdentifier(communicationUser7), RoleType.Consumer);
            UpdateRoomParticipants.Add(updateRoomParticipant1);
            UpdateRoomParticipants.Add(updateRoomParticipant2);
            Response<ParticipantsCollection> updateParticipantsResponse = await roomsClient.UpdateParticipantsAsync(roomId, updateRoomParticipants);
            ParticipantsCollection roomParticipantsAfterUpdatingParticipants = updateParticipantsResponse.Value;

            // Update Room Participants
            List<CommunicationIdentifier> RemoveRoomParticipants = new List<CommunicationIdentifier>();
            RemoveRoomParticipants.Add(new CommunicationUserIdentifier(communicationUser6));
            Response<ParticipantsCollection> removeParticipantsResponse = await roomsClient.RemoveParticipantsAsync(roomId, RemoveRoomParticipants);
            ParticipantsCollection roomParticipantsAfterRemovingParticipants = removeParticipantsResponse.Value;

            // Get Room Participants
            RemoveRoomParticipants.Add(new CommunicationUserIdentifier(communicationUser6));
            Response<ParticipantsCollection> getParticipantsResponse = await roomsClient.GetParticipantsAsync(roomId);
            ParticipantsCollection roomParticipants = getParticipantsResponse.Value;

            // Delete Room
            Response deleteRoomResponse = await roomsClient.DeleteRoomAsync(roomId);
        }
    }
}

