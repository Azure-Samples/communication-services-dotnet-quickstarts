// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Web;

namespace IncomingCallRouting.EventHandler
{
    /// <summary>
    /// Authorize incoming callbacks.
    /// </summary>
    public class EventAuthHandler
    {
        private static readonly string SecretKey = "secret";
        private readonly string SecretValue;

        public EventAuthHandler(string secretValue)
        {
            SecretValue = secretValue;
        }

        public bool Authorize(string query)
        {
            if (query == null)
                return false;

            return !string.IsNullOrEmpty(query) && query.Equals(SecretValue);
        }

        public string GetSecretQuerystring => $"{SecretKey}={HttpUtility.UrlEncode(SecretValue)}";
    }
}
