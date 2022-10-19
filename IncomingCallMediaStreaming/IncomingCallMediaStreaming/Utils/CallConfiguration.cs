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
        /// <summary>
        /// The connectionstring of Azure Communication Service resource.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// The base url of the applicaiton.
        /// </summary>
        public string AppBaseUrl { get; private set; }

        /// <summary>
        /// The callback url of the application where notification would be received.
        /// </summary>
        public string AppCallbackUrl { get; private set; }

        /// <summary>
        /// The publicly available url of the audio file which would be played as a prompt.
        /// </summary>
        public string AudioFileUrl { get; private set; }

        /// <summary>
        /// The publicly available participant id to transfer the incoming call.
        /// </summary>
        public string MediaStreamingTransportURI { get; private set; }

        public CallConfiguration(string connectionString, string appBaseUrl, string audioFileUri, string mediaStreamingTransportURI, string queryString)
        {
            ConnectionString = connectionString;
            AppBaseUrl = appBaseUrl;
            AudioFileUrl = audioFileUri;
            AppCallbackUrl = $"{AppBaseUrl}/CallAutomationApiCallBack?{queryString}";
            MediaStreamingTransportURI = mediaStreamingTransportURI;
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
    }
}
