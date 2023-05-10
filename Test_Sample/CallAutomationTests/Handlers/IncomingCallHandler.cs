// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using static CallAutomation.Scenarios.Recognize;

namespace CallAutomation.Scenarios
{
    /// <summary>
    /// Handling different callback events
    /// and perform operations
    /// </summary>

    public class IncomingCallHandler
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly IConfiguration _configuration;
        private static int identificationValidationTrial = 0;
        private static int authenticationTrial = 0;

        public IncomingCallHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            _callAutomationClient = new CallAutomationClient(_configuration["ConnectionString"]);
        }

        public async Task<IResult> HandleIncomingCall(EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                Logger.LogInformation("Event " + JsonConvert.SerializeObject(eventGridEvent));

                // Handle system events
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the subscription validation event.
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return Results.Ok(responseData);
                    }
                }
                else
                {
                    Logger.LogInformation($"AnswerCall - {JsonNode.Parse(eventGridEvent.Data)}");
                    var jsonObject = JsonNode.Parse(eventGridEvent.Data)!.AsObject();
                    return await AnswerCall(jsonObject);
                }
            }
            return Results.Ok();
        }

        public async Task<IResult> HandleCallback(CloudEvent[] cloudEvents, string callerId)
        {
            CallConnection? callConnection = null;

            foreach (var cloudEvent in cloudEvents)
            {
                var @event = CallAutomationEventParser.Parse(cloudEvent);
                Logger.LogInformation($"Event received: {@event.GetType().ToString()}");

                if (callConnection == null)
                {
                    callConnection = _callAutomationClient.GetCallConnection(@event.CallConnectionId);
                }

                if (@event is CallConnected)
                {
                    // Identify Caller 
                    Logger.LogInformation($"callerId : {callerId}");
                    var validated = ValidateCaller(callerId);
                    if (validated)
                    {
                        // Authenticate
                        authenticationTrial++;
                        await Recognize.StartRecognizingDtmf(callerId, _configuration, callConnection, RecognizeFor.Autentication);
                    }
                    else
                    {
                        // recognize for phone/customer id
                        identificationValidationTrial ++;
                        await Recognize.StartRecognizingDtmf(callerId, _configuration, callConnection, RecognizeFor.Identification);
                    }
                }
                if (@event is RecognizeCompleted { OperationContext: "Identification" })
                {
                    var recognizeCompleted = (RecognizeCompleted)@event;
                    var collectTonesResult = (CollectTonesResult) recognizeCompleted.RecognizeResult;
                    var inputCallerId = Recognize.CombineDtmfTones(collectTonesResult.Tones);
                    Logger.LogInformation($"input CallerId : {inputCallerId}");

                    var validated = ValidateCaller(inputCallerId);
                    if (validated)
                    {
                        // Authenticate
                        authenticationTrial++;
                        await Recognize.StartRecognizingDtmf(callerId, _configuration, callConnection, RecognizeFor.Autentication);
                    }
                    else
                    {
                        if(identificationValidationTrial < 3)
                        {
                            identificationValidationTrial++;
                            await Recognize.StartRecognizingDtmf(callerId, _configuration, callConnection, RecognizeFor.Identification);
                        }
                        else
                        {
                            // Identification Validation failed 3 times
                            // play UnAuthorized audio
                            await PlayAudio.PlayAudioToAll(new PlayOptions() { Loop = false, OperationContext = "SimpleIVR" }, PlayAudio.AudioText.UnIdentifiedUser,
                                _configuration, callConnection);
                        }
                    }
                }
                if (@event is RecognizeCompleted { OperationContext: "Authentication" })
                {
                    var recognizeCompleted = (RecognizeCompleted)@event;
                    var collectTonesResult = (CollectTonesResult)recognizeCompleted.RecognizeResult;
                    var authPin = Recognize.CombineDtmfTones(collectTonesResult.Tones);
                    Logger.LogInformation($"auth Pin : {authPin}");

                    var authenticated = AuthenticateCaller(authPin);
                    if (authenticated)
                    {
                        // Start Recognize
                        await Recognize.StartRecognizingChoice(callerId, _configuration, callConnection);
                    }
                    else
                    {
                        // Retry Authenticate
                        if(authenticationTrial < 3)
                        {
                            authenticationTrial++;
                            await Recognize.StartRecognizingDtmf(callerId, _configuration, callConnection, RecognizeFor.Autentication);
                        }
                        else
                        {
                            // play UnAuthorized audio
                            await PlayAudio.PlayAudioToAll(new PlayOptions() { Loop = false, OperationContext = "SimpleIVR" }, PlayAudio.AudioText.UnIdentifiedUser,
                                _configuration, callConnection);
                        }
                    }
                }
                if (@event is RecognizeCompleted { OperationContext: "MainMenu" })
                {
                    //Perform operation as per DTMF tone recieved
                    var recognizeCompleted = (RecognizeCompleted)@event;
                    var recognizeCompletedEvent = (RecognizeCompleted)@event;
                    Logger.LogInformation($"RecognizeCompleted event received  ===  {recognizeCompletedEvent.RecognizeResult}");

                    switch (recognizeCompletedEvent.RecognizeResult)
                    {
                        // Take action for Recongition through Choices
                        case ChoiceResult choiceResult:
                            await PlayAudio.PlayAudioOperation(choiceResult.Label, _configuration, callConnection);
                            break;
                        //Take action for Recongition through DTMF
                        case CollectTonesResult collectTonesResult:
                            var tones = collectTonesResult.Tones;
                            await PlayAudio.PlayAudioOperation(tones[0], _configuration, callConnection);
                            break;
                        default:
                            Logger.LogError($"Unexpected recognize event result identified for connection id: {@event.CallConnectionId}");
                            break;
                    }
                }
                if (@event is RecognizeFailed)
                {
                    // play invalid audio
                    await PlayAudio.PlayAudioToAll(new PlayOptions() { Loop = false, OperationContext = "SimpleIVR" }, PlayAudio.AudioText.InvalidAudio,
                        _configuration, callConnection);
                }
                if (@event is PlayCompleted { OperationContext: "SimpleIVR" })
                {
                    _ = await callConnection.HangUpAsync(true);
                }
                if (@event is PlayFailed { OperationContext: "SimpleIVR" })
                {
                    // play invalid audio
                    await PlayAudio.PlayAudioToAll(new PlayOptions() { Loop = false, OperationContext = "SimpleIVR" }, PlayAudio.AudioText.InvalidAudio,
                        _configuration, callConnection);
                }
                if (@event is AddParticipantSucceeded)
                {
                    Logger.LogInformation("Successfully added Agent participant");
                }
                if (@event is AddParticipantFailed)
                {
                    Logger.LogError("Failed to add Agent participant");
                    _ = await callConnection.HangUpAsync(true);
                }
            }
            return Results.Ok();
        }

        public async Task<IResult> AnswerCall(JsonObject jsonObject)
        {
            if (jsonObject != null && _callAutomationClient != null)
            {
                var callerId = jsonObject["from"]!["rawId"]!.ToString();
                var incomingCallContext = jsonObject["incomingCallContext"]!.ToString();
                var callbackUri = new Uri(_configuration["BaseUri"] + $"/api/calls/{Guid.NewGuid()}?callerId={callerId}");

                Logger.LogInformation($" CognitiveServiceEndpointUrl : {_configuration["CognitiveServiceEndpointUrl"]}");
                // Answer Call
                var answerCallOptions = new AnswerCallOptions(incomingCallContext, callbackUri)
                {
                    AzureCognitiveServicesEndpointUrl = new Uri(_configuration["CognitiveServiceEndpointUrl"]!)
                };

                var response = await _callAutomationClient.AnswerCallAsync(answerCallOptions);
                Logger.LogInformation($"AnswerCallAsync Response -----> {response.GetRawResponse()}");

                return Results.Ok();
            }
            return Results.Problem("Answer Call failed.");

        }

        private bool ValidateCaller(string callerId)
        {
            // make a call to customers service to get the status
            // fake implementation from config 
            if (_configuration["CallerId"] == callerId)
            {
                return true;
            }
            return false;
        }

        private bool AuthenticateCaller(string authPin)
        {
            // make a call to customers service to validate pin
            // fake implementation from config 
            if (_configuration["AuthPin"] == authPin)
            {
                return true;
            }
            return false;
        }
    }
}
