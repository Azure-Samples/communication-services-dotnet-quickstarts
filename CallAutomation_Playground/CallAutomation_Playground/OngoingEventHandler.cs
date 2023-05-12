using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground
{
    /// <summary>
    /// This demonstrate how to attach ongoing event handler callback for Call Automation
    /// In below example, adding callback function that will execute whenever specific event type is recieved for that call
    /// OngoingEventProcessor could be also very useful for callback design pattern
    /// </summary>
    public class OngoingEventHandler : IOngoingEventHandler
    {
        private readonly ILogger<OngoingEventHandler> _logger;
        private readonly CallAutomationEventProcessor _eventProcessor;

        public OngoingEventHandler(
            ILogger<OngoingEventHandler> logger,
            CallAutomationClient callAutomation)
        {
            _logger = logger;
            _eventProcessor = callAutomation.GetEventProcessor();
        }

        /// <summary>
        /// Update and write whenever participant number is updated.
        /// </summary>
        public void AttachCountParticipantsInTheCall(string callConnectionId)
        {
            _eventProcessor.AttachOngoingEventProcessor<ParticipantsUpdated>(callConnectionId, recievedEvent => {
                _logger.LogInformation($"Number of participants in this Call: [{callConnectionId}], Number Of Participants[{recievedEvent.Participants.Count}]");
            });
        }

        /// <summary>
        /// Whenever the call ends (i.e. Call Automation leaves the call or Call is terminated),
        /// Notify that Call automation has lost the control of the call because of it.
        /// </summary>
        public void AttachDisconnectedWrapup(string callConnectionId)
        {
            _eventProcessor.AttachOngoingEventProcessor<CallDisconnected>(callConnectionId, recievedEvent => {
                _logger.LogInformation($"Call is disconnected!: [{callConnectionId}]");
            });
        }
    }
}
