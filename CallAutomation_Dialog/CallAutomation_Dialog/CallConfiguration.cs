using Azure.Communication.Identity;
using Azure.Communication;

namespace CallAutomation_Dialog
{
    /// <summary>
    /// Configuration assoociated with the call.
    /// </summary>
    public class CallConfiguration
    {
        public CallConfiguration()
        {

        }

        /// <summary>
        /// The connectionstring of Azure Communication Service resource.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The bot id.
        /// </summary>
        public Dictionary<string, string> BotRouting { get; set; }

        /// <summary>
        /// The default bot id.
        /// </summary>
        public string DefaultBotId { get; set; }
    }
}