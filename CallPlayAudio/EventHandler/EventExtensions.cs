// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Communication.CallingServer.Sample.CallPlayAudio
{
    using Azure.Communication.CallAutomation;
    using System;

    public class NotificationCallback
    {
        public Action<CallAutomationEventBase> Callback { get; set; }

        public NotificationCallback(Action<CallAutomationEventBase> callBack)
        {
            this.Callback = callBack;
        }
    }
}
