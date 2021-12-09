// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IncomingCallRouting
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
            SecretValue = "h3llowW0rld";
        }

        public static bool Authorize(string query)
        {
            if (query == null)
                return false;

            return !string.IsNullOrEmpty(query) && query.Equals(SecretValue);
        }

        public static string GetSecretQuerystring => $"{SecretKey}={HttpUtility.UrlEncode(SecretValue)}";
    }
}
