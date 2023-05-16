﻿using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Controllers;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground
{
    /// <summary>
    /// This is our top level menu that will have our greetings menu.
    /// </summary>
    public class TopLevelMenuService : ITopLevelMenuService
    {
        private readonly ILogger<TopLevelMenuService> _logger;
        private readonly CallAutomationClient _callAutomation;
        private readonly PlaygroundConfigs _playgroundConfig;

        public TopLevelMenuService(
            ILogger<TopLevelMenuService> logger, 
            CallAutomationClient callAutomation, 
            PlaygroundConfigs playgroundConfig)
        {
            _logger = logger;
            _callAutomation = callAutomation;
            _playgroundConfig = playgroundConfig;
        }

        public async Task InvokeTopLevelMenu(
            CommunicationIdentifier originalTarget, 
            CallConnection callConnection,
            string serverCallId)
        {
            _logger.LogInformation($"Invoking top level menu, with CallConnectionId[{callConnection.CallConnectionId}]");

            // prepare calling modules to interact with this established call
            ICallingModules callingModule = new CallingModules(callConnection, _playgroundConfig);

            try
            {
                while(true)
                {
                    // Top Level DTMF Menu, ask for which menu to be selected
                    string selectedTone = await callingModule.RecognizeTonesAsync(
                        originalTarget,
                        1,
                        1,
                        _playgroundConfig.AllPrompts.MainMenu,
                        _playgroundConfig.AllPrompts.Retry);

                    _logger.LogInformation($"Caller selected DTMF Tone[{selectedTone}]");

                    switch (selectedTone)
                    {
                        // Option 1: Collect phone number, then add that person to the call.
                        case "1":
                            // recognize phonenumber
                            string phoneNumberToCall = await callingModule.RecognizeTonesAsync(
                                originalTarget,
                                10,
                                10,
                                _playgroundConfig.AllPrompts.CollectPhoneNumber,
                                _playgroundConfig.AllPrompts.Retry);

                            string formattedPhoneNumber = Tools.FormatPhoneNumbers(phoneNumberToCall);
                            PhoneNumberIdentifier phoneIdentifierToAdd = new PhoneNumberIdentifier(formattedPhoneNumber);
                            _logger.LogInformation($"Phonenumber to Call[{formattedPhoneNumber}]");

                            // then add the phone number
                            await callingModule.AddParticipantAsync(
                                phoneIdentifierToAdd,
                                _playgroundConfig.AllPrompts.AddParticipantSuccess,
                                _playgroundConfig.AllPrompts.AddParticipantFailure,
                                _playgroundConfig.AllPrompts.Music);

                            _logger.LogInformation($"Add Participant finished.");
                            break;

                        // Option 2: Remove all participants in the call except the original caller.
                        case "2":
                            _logger.LogInformation($"Removing all participants");

                            await callingModule.RemoveAllParticipantExceptCallerAsync(originalTarget);
                            break;

                        // Option 3: Transfer Call to another PSTN endpoint
                        case "3":
                            // recognize phonenumber to transfer to
                            string phoneNumberToTransfer = await callingModule.RecognizeTonesAsync(
                                originalTarget,
                                10,
                                10,
                                _playgroundConfig.AllPrompts.CollectPhoneNumber,
                                _playgroundConfig.AllPrompts.Retry);

                            string formattedTransferNumber = Tools.FormatPhoneNumbers(phoneNumberToTransfer);
                            PhoneNumberIdentifier phoneIdentifierToTransfer = new PhoneNumberIdentifier(formattedTransferNumber);
                            _logger.LogInformation($"Phonenumber to Transfer[{formattedTransferNumber}]");

                            // then transfer to the phonenumber
                            var trasnferSuccess = await callingModule.TransferCallAsync(
                                phoneIdentifierToTransfer,
                                _playgroundConfig.AllPrompts.TransferFailure);

                            if (trasnferSuccess)
                            {
                                _logger.LogInformation($"Successful Transfer - ending this logic.");
                                return;
                            }
                            else
                            {
                                _logger.LogInformation($"Transfer Failed - back to main menu.");
                            }
                            break;

                        // Option 4: Start Recording this call
                        case "4":
                            // ... then Start Recording
                            // this will accept serverCallId and uses main service client
                            _logger.LogInformation($"Start Recording...");
                            CallLocator callLocator = new ServerCallLocator(serverCallId);
                            StartRecordingOptions startRecordingOptions = new StartRecordingOptions(callLocator);
                            _ = await _callAutomation.GetCallRecording().StartAsync(startRecordingOptions);

                            // Play message of start of recording
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_playgroundConfig.AllPrompts.PlayRecordingStarted);
                            break;

                        // Option 5: Play Message and terminate the call
                        case "5":
                            _logger.LogInformation($"Terminating Call. Due to wrong input too many times, exception happened, or user requested termination.");
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_playgroundConfig.AllPrompts.Goodbye);
                            await callingModule.TerminateCallAsync();
                            return;

                        default:
                            // Wrong input!
                            // play message then retry this toplevel menu.
                            _logger.LogInformation($"Wrong Input! selectedTone[{selectedTone}]");
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_playgroundConfig.AllPrompts.Retry);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Exception during Top Level Menu! [{e}]");
            }

            // wrong input too many times, exception happened, or user requested termination.
            // good bye and hangup
            _logger.LogInformation($"Terminating Call. Due to wrong input too many times, exception happened, or user requested termination.");
            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_playgroundConfig.AllPrompts.Goodbye);
            await callingModule.TerminateCallAsync();
            return;
        }
    }
}
