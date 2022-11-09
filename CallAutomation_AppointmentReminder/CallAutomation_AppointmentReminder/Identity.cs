using Azure.Communication;
using Azure.Communication.Identity;

namespace CallAutomation_AppointmentReminder
{
    public static class Identity
    {
        public static async Task<CommunicationUserIdentifier> CreateUser(string connectionString)
        {
            var client = new CommunicationIdentityClient(connectionString);
            var user = await client.CreateUserAsync().ConfigureAwait(false);
            return user.Value;
        }
    }
}
