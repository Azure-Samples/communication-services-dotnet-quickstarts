namespace CallAutomation_Playground
{
    public class PlaygroundConfig
    {
        /// <summary>
        /// public Callback URI that will be used to answer incoming call event,
        /// or handle mid-call events, such as CallConnected.
        /// See README file for details on how to setup tunnel on your localhost to handle this.
        /// </summary>
        public Uri CallbackUri { get; init; }

        /// <summary>
        /// DirectOffered phonenumber is can be aquired from Azure Communication Service portal.
        /// In order to answer Incoming PSTN call or make an outbound call to PSTN number,
        /// Call Automation needs Directly offered PSTN number to do these actions.
        /// </summary>
        public string DirectOfferedPhonenumber { get; init; }

        /// <summary>
        /// List of all prompts from this sample's business logic.
        /// These recorded prompts must be uploaded to publicily available Uri endpoint.
        /// See README for pre-generated samples that can be used for demo.
        /// </summary>
        public Prompts AllPrompts { get; init; }

        public class Prompts
        {
            public Uri MainMenu { get; init; }

            public Uri CollectPhoneNumber { get; init; }

            public Uri Retry { get; init; }

            public Uri AddParticipantSuccess { get; init; }

            public Uri AddParticipantFailure { get; init; }

            public Uri TransferFailure { get; init; }

            public Uri PlayRecordingStarted { get; init; }

            public Uri Goodbye { get; init; }

            public Uri Music { get; init; }
        }
    }
}
