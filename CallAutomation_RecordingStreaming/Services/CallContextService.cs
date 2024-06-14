// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using RecordingStreaming.Models;
using System.Collections.Concurrent;

namespace RecordingStreaming.Services
{
    public static class CallContextService
    {
        public static ConcurrentDictionary<string, string> MediaSubscriptionIdsToServerCallId = new();
        public static ConcurrentDictionary<string, string> CallConnectionIdsToServerCallId = new();
        public static ConcurrentDictionary<string, string> CallIdsToServerCallId = new();
        private static ConcurrentDictionary<string, ActiveCall> _serverCallIdToCall = new();

        public static ActiveCall? GetActiveCall(string serverCallId)
        {
            if (_serverCallIdToCall.TryGetValue(serverCallId, out var activeCall))
            {
                return activeCall;
            }

            return null;
        }

        public static ActiveCall SetActiveCall(string serverCallId, ActiveCall activeCall)
        {
            return _serverCallIdToCall.AddOrUpdate(serverCallId, activeCall, (_, oldActiveCall) =>
                new ActiveCall
                {
                    CallConnection = activeCall.CallConnection ?? oldActiveCall.CallConnection,
                    CallConnectionProperties = activeCall.CallConnectionProperties ?? oldActiveCall.CallConnectionProperties,
                    CallId = activeCall.CallId ?? oldActiveCall.CallId,
                    Stream = activeCall.Stream ?? oldActiveCall.Stream,
                    StartRecordingTimer = activeCall.StartRecordingTimer ?? oldActiveCall.StartRecordingTimer,
                    StopRecordingTimer = activeCall.StopRecordingTimer ?? oldActiveCall.StopRecordingTimer,
                    CallConnectedTimer = activeCall.CallConnectedTimer ?? oldActiveCall.CallConnectedTimer,
                    SubscriptionId = activeCall.SubscriptionId ?? oldActiveCall.SubscriptionId,
                    StartRecordingWithAnswerTimer = activeCall.StartRecordingWithAnswerTimer ?? oldActiveCall.StartRecordingWithAnswerTimer,
                    StartRecordingEventTimer = activeCall.StartRecordingEventTimer ?? oldActiveCall.StartRecordingEventTimer
                });
        }

        public static bool RemoveActiveCall(string serverCallId)
        {
            return _serverCallIdToCall.TryRemove(serverCallId, out _);
        }
    }
}
