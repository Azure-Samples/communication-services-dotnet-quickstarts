// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Azure.Communication.CallingServer;

namespace IncomingCallRouting.EventHandler
{
    /// <summary>
    /// Maintaining callback object.
    /// </summary>
    public class NotificationCallback
    {
        public Action<CallAutomationEventBase> Callback { get; set; }

        public NotificationCallback(Action<CallAutomationEventBase> callBack)
        {
            this.Callback = callBack;
        }
    }
}
