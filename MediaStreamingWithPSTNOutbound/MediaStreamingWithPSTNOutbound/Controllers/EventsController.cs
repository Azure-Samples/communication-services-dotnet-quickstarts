using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace incoming_call_recording.Controllers
{
    [Route("api")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private readonly CallAutomationClient callAutomationClient;
        private string hostUrl = "";
        private string cognitiveServiceEndpoint = "";
        private string transportUrl = "";
        private string targetPhoneNumber = "";
        private string acsPhoneNumber = "";
        private string acsTargetUser = "";
        private static string acsRecordingId = "";

        public EventsController(ILogger<EventsController> logger
            , IConfiguration configuration,
            CallAutomationClient callAutomationClient)
        {
            //Get ACS Connection String from appsettings.json
            this.hostUrl = configuration.GetValue<string>("BaseUrl");
            this.cognitiveServiceEndpoint = configuration.GetValue<string>("CognitiveServiceEndpoint");
            this.transportUrl = configuration.GetValue<string>("TransportUrl");
            this.acsPhoneNumber = configuration.GetValue<string>("AcsPhoneNumber");
            this.targetPhoneNumber = configuration.GetValue<string>("TargetPhoneNumber");
            this.acsTargetUser = configuration.GetValue<string>("AcsTargetUser");
            ArgumentException.ThrowIfNullOrEmpty(this.hostUrl);
            //Call Automation Client
            this.callAutomationClient = callAutomationClient;
            this.logger = logger;
            this.configuration = configuration;
        }

        [HttpPost]
        [Route("createOutBoundCall")]
        public async Task<IActionResult> CreatePSTNCall()
        {
            PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
            PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);
            var callbackUri = new Uri(this.hostUrl + $"/api/callbacks/{Guid.NewGuid()}");
            var mediaStreamingOptions = new MediaStreamingOptions(
                        new Uri(this.transportUrl),
                          MediaStreamingTransport.Websocket,
                          MediaStreamingContent.Audio,
                          MediaStreamingAudioChannel.Mixed
                          );
            CallInvite callInvite = new CallInvite(target, caller);
            var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) },
                MediaStreamingOptions = mediaStreamingOptions
            };

            CreateCallResult createCallResult = await this.callAutomationClient.CreateCallAsync(createCallOptions);
            logger.LogInformation($"Created call with the target: {target}, Connection State: {createCallResult.CallConnectionProperties?.CallConnectionState}," +
                $" ConnectionId: {createCallResult.CallConnectionProperties?.CallConnectionId}");
            return Ok();
        }

        /* Route for Azure Communication Service eventgrid webhooks*/
        [HttpPost]
        [Route("callbacks/{contextid}")]
        public async Task<IActionResult> Handle([FromBody] CloudEvent[] cloudEvents)
        {
            var eventProcessor = this.callAutomationClient.GetEventProcessor();
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, correlationId: {corId}, serverCallId: {serverId}, time: {datetime}",
                    parsedEvent?.GetType(),
                    parsedEvent?.CallConnectionId,
                    parsedEvent?.CorrelationId,
                    parsedEvent?.ServerCallId,
                    DateTime.Now);


                if (parsedEvent is CallConnected callConnected)
                {
                    logger.LogInformation($"Call connected event received for correlation id: {callConnected.CorrelationId}");
                    var acsTarget = new CommunicationUserIdentifier(acsTargetUser);
                    CallInvite addParticipantInvite = new CallInvite(acsTarget);

                    var addParticipantOptions = new AddParticipantOptions(addParticipantInvite)
                    {
                        InvitationTimeoutInSeconds = 30
                    };
                    var addParticipantResult = await callAutomationClient.GetCallConnection(callConnected.CallConnectionId).AddParticipantAsync(addParticipantOptions);
                    logger.LogInformation($"Adding Participant to the call: {addParticipantResult.Value?.InvitationId}");
                }
                else if (parsedEvent is AddParticipantSucceeded addParticipantSucceeded)
                {
                    logger.LogInformation($"Received AddParticipantSucceeded event ");
                    var response = await callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId).GetParticipantsAsync();
                    var participantCount = response.Value.Count;
                    var participantList = response.Value;

                    logger.LogInformation($"Total participants in call: {participantCount}");
                    logger.LogInformation($"Participants: {JsonSerializer.Serialize(participantList)}");
                }
                else if (parsedEvent is AddParticipantFailed addParticipantFailed)
                {
                    logger.LogInformation($"Add participant failed in call ParticipantId: {addParticipantFailed.Participant.RawId}, " +
                        $"ResultInformation: {addParticipantFailed.ResultInformation} ");
                }
                else if (parsedEvent is MediaStreamingStarted mediaStreamingStarted)
                {
                    logger.LogInformation($"Mediastreaming started status: {mediaStreamingStarted.MediaStreamingUpdate?.MediaStreamingStatus}, " +
                        $"status details: {mediaStreamingStarted.MediaStreamingUpdate?.MediaStreamingStatusDetails}");
                }
                else if (parsedEvent is MediaStreamingFailed mediaStreamingFailed)
                {
                    logger.LogInformation($"Mediastreaming failed status: {mediaStreamingFailed.MediaStreamingUpdate?.MediaStreamingStatus}, " +
                        $"status details: {mediaStreamingFailed.MediaStreamingUpdate?.MediaStreamingStatusDetails}");

                    logger.LogInformation($"Mediastreaming failed ResultInformation: {mediaStreamingFailed.ResultInformation}");
                }
                else if (parsedEvent is MediaStreamingStopped mediaStreamingStopped)
                {
                    logger.LogInformation($"Mediastreaming failed status: {mediaStreamingStopped.MediaStreamingUpdate?.MediaStreamingStatus}, " +
                        $"status details: {mediaStreamingStopped.MediaStreamingUpdate?.MediaStreamingStatusDetails}");

                    logger.LogInformation($"Mediastreaming failed ResultInformation: {mediaStreamingStopped.ResultInformation}");
                }

            }

            eventProcessor.ProcessEvents(cloudEvents);
            return Ok();
        }
    }
}
