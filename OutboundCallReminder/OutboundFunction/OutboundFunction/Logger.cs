// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace OutboundFunction
{
    //Caution: Logging should be removed/disabled if you want to use this sample in production to avoid exposing sensitive information
    public static class Logger
    {
        public enum MessageType
        {
            INFORMATION,
            ERROR
        }

        public static ILogger logger = null;

        /// <summary>
        /// Log message to console
        /// </summary>
        /// <param name="messageType">Type of the message: Information or Error</param>
        /// <param name="message">Message string</param>
        public static void LogMessage(MessageType messageType, string message)
        {
            if(messageType == MessageType.ERROR)
                logger.LogError(message);
            else
                logger.LogInformation(message);
        }
    }
}
