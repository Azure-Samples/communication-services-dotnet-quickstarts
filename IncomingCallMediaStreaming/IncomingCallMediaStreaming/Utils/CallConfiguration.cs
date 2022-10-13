// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace IncomingCallMediaStreaming
{
    /// <summary>
    /// Configuration assoociated with the call.
    /// </summary>
    public class CallConfiguration
    {
        private static CallConfiguration callConfiguration = null;
        public CallConfiguration(string connectionString, string appBaseUrl, string audioFileUri, string mediaStreamingTransportURI, string queryString)
        {
            this.ConnectionString = connectionString;
            this.AppBaseUrl = appBaseUrl;
            this.AudioFileUrl = audioFileUri;
            this.AppCallbackUrl = $"{AppBaseUrl}/CallingServerAPICallBacks?{queryString}";
            this.mediaStreamingTransportURI = mediaStreamingTransportURI;
        }

        public static CallConfiguration GetCallConfiguration(IConfiguration configuration, string queryString)
        {
            if(callConfiguration == null)
            {
                callConfiguration = new CallConfiguration(configuration["ResourceConnectionString"],
                    configuration["AppCallBackUri"],
                    configuration["AudioFileUri"],
                    configuration["MediaStreamingTransportURI"],
                    queryString);
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
        public string mediaStreamingTransportURI { get; private set; }
    }
}
