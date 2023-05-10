// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace OutboundFunction
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Web;

    public class EventAuthHandler
    {
        private static readonly string SecretKey = "secret";
        private static readonly string SecretValue;

        static EventAuthHandler()
        {
            SecretValue = Environment.GetEnvironmentVariable("SecretPlaceholder") ?? "h3llowW0rld";
        }

        public static bool Authorize(HttpRequest request)
        {
            string secretKeyValue = request.Query[SecretKey].ToString();
            return !string.IsNullOrEmpty(secretKeyValue) && secretKeyValue.Equals(SecretValue);
        }

        public static string GetSecretQuerystring => $"{SecretKey}={HttpUtility.UrlEncode(SecretValue)}";
    }
}
