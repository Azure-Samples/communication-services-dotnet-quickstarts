// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Communication.Server.Calling.Sample.OutboundCallReminder
{
    using System.Configuration;
    using System.Net.Http;
    using System.Web;

    public class EventAuthHandler
    {
        private static readonly string SecretKey = "secret";
        private static readonly string SecretValue;

        static EventAuthHandler()
        {
            SecretValue = ConfigurationManager.AppSettings["SecretPlaceholder"] ?? "h3llowW0rld";
        }

        public static bool Authorize(HttpRequestMessage request)
        {
            if (request?.RequestUri?.Query == null)
                return false;

            var keyValuePair = HttpUtility.ParseQueryString(request.RequestUri.Query);

            return !string.IsNullOrEmpty(keyValuePair[SecretKey]) && keyValuePair[SecretKey].Equals(SecretValue);
        }

        public static string GetSecretQuerystring => $"{SecretKey}={HttpUtility.UrlEncode(SecretValue)}";
    }
}
