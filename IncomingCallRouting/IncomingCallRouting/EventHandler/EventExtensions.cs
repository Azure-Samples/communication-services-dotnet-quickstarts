// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IncomingCallRouting.Events;

namespace IncomingCallRouting
{
    /// <summary>
    /// Maintaining callback object.
    /// </summary>

    using Azure.Communication.CallingServer;
    using System;

    public class NotificationCallback
    {
        public Action<CallingServerEventBase> Callback { get; set; }

        public NotificationCallback(Action<CallingServerEventBase> callBack)
        {
            this.Callback = callBack;
        }
    }
}
