// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IncomingCallRouting
{
   public static class Constants
    {
        public const string userIdentityRegex = @"8:acs:[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}_[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
        public const string phoneIdentityRegex = @"^\+\d{10,14}$";
    }
}
