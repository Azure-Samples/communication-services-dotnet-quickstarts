// © Microsoft Corporation. All rights reserved.

using Azure;
using Azure.Communication;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CallAutomation
{
    public class UserTokenController : Controller
    {
        private readonly CommunicationIdentityClient _client;
        private readonly RoomsClient _roomClient;

        static string roomId = string.Empty;
        static List<UsersForRoom> listCommunicationUserIdentifierAndToken = new List<UsersForRoom>();
        public UserTokenController(IConfiguration configuration)
        {
            _client = new CommunicationIdentityClient(configuration["ResourceConnectionString"]);
            _roomClient = new RoomsClient(configuration["ResourceConnectionString"]);
        }

        /// <summary>
        /// Gets a token to be used to initalize the call client
        /// </summary>
        /// <returns></returns>
        [Route("/token")]
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            try
            {
                Response<CommunicationUserIdentifierAndToken> response = await _client.CreateUserAndTokenAsync(scopes: new[] { CommunicationTokenScope.VoIP });

                var responseValue = response.Value;

                var jsonFormattedUser = new
                {
                    communicationUserId = responseValue.User.Id
                };

                var clientResponse = new
                {
                    user = jsonFormattedUser,
                    token = responseValue.AccessToken.Token,
                    expiresOn = responseValue.AccessToken.ExpiresOn
                };

                return this.Ok(clientResponse);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error occured while Generating Token: {ex}");
                return this.Ok(this.Json(ex));
            }
        }

        /// <summary>
        /// Gets a token to be used to initalize the call client
        /// </summary>
        /// <returns></returns>
        [Route("/refreshToken/{identity}")]
        [HttpGet]
        public async Task<IActionResult> GetAsync(string identity)
        {
            try
            {
                CommunicationUserIdentifier identifier = new CommunicationUserIdentifier(identity);
                Response<AccessToken> response = await _client.GetTokenAsync(identifier, scopes: new[] { CommunicationTokenScope.VoIP });

                var responseValue = response.Value;
                var clientResponse = new
                {
                    token = responseValue.Token,
                    expiresOn = responseValue.ExpiresOn
                };

                return this.Ok(clientResponse);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error occured while Generating Token: {ex}");
                return this.Ok(this.Json(ex));
            }
        }

        /// <summary>
        /// Gets a token to be used to initalize the call client
        /// </summary>
        /// <returns></returns>
        [Route("/usersForRoom")]
        [HttpGet]
        public async Task<IActionResult> GetUsersForRoomAsync()
        {
            try
            {

                if (listCommunicationUserIdentifierAndToken.Count > 0)
                {
                    return this.Ok(listCommunicationUserIdentifierAndToken);
                }
                else
                    for (int i = 4; i > 0; i--)
                    {
                        Response<CommunicationUserIdentifierAndToken> response = await _client.CreateUserAndTokenAsync(scopes: new[] { CommunicationTokenScope.VoIP });

                        var responseValue = response.Value;

                        var user = new CommunicationUserIdentifier(responseValue.User.Id);


                        var clientResponse = new UsersForRoom
                        {
                            User = user.Id,
                            Token = responseValue.AccessToken.Token,
                            ExpiresOn = responseValue.AccessToken.ExpiresOn
                        };

                        listCommunicationUserIdentifierAndToken.Add(clientResponse);
                    }

                await SetUpRoom();

                return this.Ok(listCommunicationUserIdentifierAndToken);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error occured while Generating Token: {ex}");
                return this.Ok(this.Json(ex));
            }
        }

        /// <summary>
        /// Set selected users for room
        /// </summary>
        /// <returns></returns>
        [Route("/getRoomId")]
        public string getRoomId(string user)
        {
            return roomId;
        }

        /// <summary>
        /// Set selected users for room
        /// </summary>
        /// <returns></returns>
        [Route("/setSelectedUsersForRoom/{user}")]
        public void SetSelectedUsersForRoom(string user)
        {
            listCommunicationUserIdentifierAndToken.RemoveAll(a => a.User.ToString() == user.ToString());
        }

        /// <summary>
        /// Set up room
        /// </summary>
        /// <returns></returns>
        private async Task SetUpRoom()
        {
            try
            {
                List<RoomParticipant> roomParticipants = new List<RoomParticipant>();
                var participant1 = new RoomParticipant(new CommunicationUserIdentifier(listCommunicationUserIdentifierAndToken[0].User), RoleType.Presenter);
                var participant2 = new RoomParticipant(new CommunicationUserIdentifier(listCommunicationUserIdentifierAndToken[1].User), RoleType.Attendee);
                var participant3 = new RoomParticipant(new CommunicationUserIdentifier(listCommunicationUserIdentifierAndToken[2].User), RoleType.Attendee);
                var participant4 = new RoomParticipant(new CommunicationUserIdentifier(listCommunicationUserIdentifierAndToken[3].User), RoleType.Attendee);

                roomParticipants.Add(participant1);
                roomParticipants.Add(participant2);
                roomParticipants.Add(participant3);
                roomParticipants.Add(participant4);

                CancellationToken cancellationToken = new CancellationTokenSource().Token;
                DateTimeOffset validFrom = DateTimeOffset.UtcNow;
                DateTimeOffset validUntil = DateTimeOffset.UtcNow.AddDays(10);

                CommunicationRoom createdRoom = await _roomClient.CreateRoomAsync(validFrom, validUntil, RoomJoinPolicy.InviteOnly, roomParticipants, cancellationToken);
                roomId = createdRoom.Id;
                //return createdRoom;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to perform rooms operations -> {ex}");
            }
        }
    }

    class UsersForRoom
    {
        public string User { get; set; }
        public string Token { get; set; }
        public DateTimeOffset ExpiresOn { get; set; }
    }
}
