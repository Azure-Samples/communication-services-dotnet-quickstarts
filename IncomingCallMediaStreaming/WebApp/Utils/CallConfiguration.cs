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
        /// The publicly available participant id to transfer the incoming call.
        /// </summary>
        public string MediaStreamingTransportURI { get; private set; }

        /// <summary>
        /// Accept Calls From particular participant ID's
        /// </summary>
        public string AcceptCallsFrom { get; private set; }

        public CallConfiguration(string connectionString, string appBaseUrl, string mediaStreamingTransportURI, string queryString, string acceptCallsFrom)
        {
            ConnectionString = connectionString;
            AppBaseUrl = appBaseUrl;
            AppCallbackUrl = $"{AppBaseUrl}/CallAutomationApiCallBack?{queryString}";
            MediaStreamingTransportURI = mediaStreamingTransportURI;
            AcceptCallsFrom = acceptCallsFrom;
        }

        public static CallConfiguration GetCallConfiguration(IConfiguration configuration, string queryString)
        {
            if(callConfiguration == null)
            {
                callConfiguration = new CallConfiguration(configuration["ResourceConnectionString"],
                    configuration["AppCallBackUri"],
                    configuration["MediaStreamingTransportURI"],
                    queryString,
                    configuration["AcceptCallsFrom"]);
            }

            return callConfiguration;
        }
    }
}
