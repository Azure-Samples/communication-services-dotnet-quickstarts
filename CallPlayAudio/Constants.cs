// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Configuration;

namespace Communication.CallingServer.Sample.CallPlayAudio
{
   public static class Constants
    {
        public const string userIdentityRegex = @"8:acs:[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}_[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
        public const string phoneIdentityRegex = @"^\+\d{10,14}$";

        public static string GetConfigSetting(string key)
        {
            string value = null;

            // check env values first
            value = Environment.GetEnvironmentVariable(key);


            if (string.IsNullOrEmpty(value))
                value = ConfigurationManager.AppSettings[key];

            return value;
        }
    }
}
