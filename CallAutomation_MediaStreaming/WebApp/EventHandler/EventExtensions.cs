// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// <summary>
/// Maintaining callback object.
/// </summary>

using Azure.Communication.CallAutomation;
using System;

namespace IncomingCallMediaStreaming
{
    public class NotificationCallback
    {
        public Action<CallAutomationEventBase> Callback { get; set; }

        public NotificationCallback(Action<CallAutomationEventBase> callBack)
        {
            this.Callback = callBack;
        }
    }
}
