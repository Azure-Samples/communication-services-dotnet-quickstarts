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
        private static CallConfiguration callConfiguration = null;
        public CallConfiguration(string connectionString, string appBaseUrl, string audioFileUri, string participant, string queryString, string ivrParticipant)
        {
            this.ConnectionString = connectionString;
            this.AppBaseUrl = appBaseUrl;
            this.AudioFileUrl = audioFileUri;
            this.AppCallbackUrl = $"{AppBaseUrl}/CallingServerAPICallBacks?{queryString}";
            targetParticipant = participant;
            this.ivrParticipant = ivrParticipant;
        }

        public static CallConfiguration GetCallConfiguration(IConfiguration configuration, string queryString)
        {
            if(callConfiguration == null)
            {
                callConfiguration = new CallConfiguration(configuration["ResourceConnectionString"],
                    configuration["AppCallBackUri"],
                    configuration["AudioFileUri"],
                    configuration["TargetParticipant"],
                    queryString,
                    configuration["IVRParticipant"]);

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
        /// The callback url of the application where notification would be received.
        /// </summary>
        public string AppCallbackUrl;

        /// <summary>
        /// The publicly available url of the audio file which would be played as a prompt.
        /// </summary>
        public string AudioFileUrl;

        /// <summary>
        /// The publicly available participant id to transfer the incoming call.
        /// </summary>
        public string targetParticipant { get; private set; }

        /// <summary>
        /// The publicly available participant id to transfer the incoming call.
        /// </summary>
        public string ivrParticipant { get; private set; }
    }
}
