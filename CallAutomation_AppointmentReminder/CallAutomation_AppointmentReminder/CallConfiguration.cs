namespace CallAutomation_AppointmentReminder
{
    /// <summary>
    /// Configuration assoociated with the call.
    /// </summary>
    public class CallConfiguration
    {
        public CallConfiguration(string connectionString, string sourceIdentity, string sourcePhoneNumber, string appBaseUrl)
        {
            this.ConnectionString = connectionString;
            this.SourceIdentity = sourceIdentity;
            this.SourcePhoneNumber = sourcePhoneNumber;
            this.AppBaseUri = appBaseUrl;
        }

        /// <summary>
        /// The connectionstring of Azure Communication Service resource.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// The source identity.
        /// </summary>
        public string SourceIdentity { get; private set; }

        /// <summary>
        /// The phone number associated with the source. 
        /// </summary>
        public string SourcePhoneNumber { get; private set; }

        /// <summary>
        /// The base url of the applicaiton.
        /// </summary>
        public string AppBaseUri;

        /// <summary>
        /// The callback url of the application where notification would be received.
        /// </summary>
        public string CallbackEventUri => $"{AppBaseUri}" + Constants.EventCallBackRoute;
    }
}
