// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace IncomingCallRouting
{
    /// <summary>
    /// Configuration assoociated with the call.
    /// </summary>
    public class CallConfiguration
    {
        public static CallConfiguration callConfiguration = null;
        public CallConfiguration(string connectionString, string appBaseUrl, string audioFileUri, string targetParticipant)
        {
            this.ConnectionString = connectionString;
            this.AppBaseUrl = appBaseUrl;
            this.AudioFileName = audioFileUri;
            targetParticipants = new Participants(targetParticipant);
        }

        public static CallConfiguration getCallConfiguration(IConfiguration configuration)
        {
            if(callConfiguration == null)
            {
                callConfiguration = new CallConfiguration(configuration["ResourceConnectionString"],
                    configuration["AppCallBackUri"],
                    configuration["AudioFileUri"],
                    configuration["TargetParticipants"]);
            }

            return callConfiguration;
        }

        /// <summary>
        /// The connectionstring of Azure Communication Service resource.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// The base url of the applicaiton.
        /// </summary>
        private string AppBaseUrl;

        /// <summary>
        /// The audio file name of the play prompt.
        /// </summary>
        private string AudioFileName;

        /// <summary>
        /// The callback url of the application where notification would be received.
        /// </summary>
        public string AppCallbackUrl => $"{AppBaseUrl}/CallingServerAPICallBacks?{EventAuthHandler.GetSecretQuerystring}";

        /// <summary>
        /// The publicly available url of the audio file which would be played as a prompt.
        /// </summary>
        public string AudioFileUrl => $"{AudioFileName}";

        public Participants targetParticipants { get; private set; }
    }
}
