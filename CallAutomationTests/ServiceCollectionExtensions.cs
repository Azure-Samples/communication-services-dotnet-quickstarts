// © Microsoft Corporation. All rights reserved.

using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using CallAutomation.Scenarios.Services;

namespace CallAutomation.Scenarios
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAllEventGridEventHandlers(this IServiceCollection services)
        {
            services.AddSingleton<IEventGridEventHandler<IncomingCallEvent>, CallEventHandler>();
            services.AddSingleton<IEventGridEventHandler<AcsRecordingFileStatusUpdatedEventData>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<AddParticipantFailed>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<AddParticipantSucceeded>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<CallConnected>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<CallDisconnected>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<CallTransferAccepted>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<CallTransferFailed>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<ParticipantsUpdated>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<PlayCompleted>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<PlayFailed>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<PlayCanceled>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<RecognizeCompleted>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<RecognizeFailed>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<RecognizeCanceled>, CallEventHandler>();
            services.AddSingleton<IEventCloudEventHandler<RecordingStateChanged>, CallEventHandler>();
            services.AddSingleton<IEventActionEventHandler<OutboundCallContext>, ActionEventHandler>();
            services.AddSingleton<IEventActionEventHandler<RecordingContext>, ActionEventHandler>();
            services.AddSingleton<EventConverter>();

            return services;
        }

        public static IServiceCollection AddRouterServices(this IServiceCollection services)
        {
            services.AddSingleton<ICallAutomationService, CallAutomationService>();
            services.AddSingleton<ICallContextService, CallContextService>();
            return services;
        }
    }
}
