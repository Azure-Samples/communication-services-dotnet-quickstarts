using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_AppointmentBooking.Controllers;    
using CallAutomation_AppointmentBooking.Interfaces;

namespace CallAutomation_AppointmentBooking
{
    /// <summary>
    /// This is our top level menu that will have our greetings menu.
    /// </summary>
    public class TopLevelMenuService : ITopLevelMenuService
    {
        private readonly ILogger<TopLevelMenuService> _logger;
        private readonly CallAutomationClient _callAutomation;
        private readonly AppointmentBookingConfig _appointmentBookingConfig;

        public TopLevelMenuService(
            ILogger<TopLevelMenuService> logger, 
            CallAutomationClient callAutomation, 
            AppointmentBookingConfig appointmentBookingConfig)
        {
            _logger = logger;
            _callAutomation = callAutomation;
            _appointmentBookingConfig = appointmentBookingConfig;
        }

        public async Task InvokeTopLevelMenu(
            CommunicationIdentifier originalTarget, 
            CallConnection callConnection,
            string serverCallId)
        {
            _logger.LogInformation($"Invoking top level menu, with CallConnectionId[{callConnection.CallConnectionId}]");

            // prepare calling modules to interact with this established call
            ICallingModules callingModule = new CallingModules(callConnection, _appointmentBookingConfig);

            try
            {
                // ... then Start Recording
                // this will accept serverCallId and uses main service client
                _logger.LogInformation($"Start Recording...");
                CallLocator callLocator = new ServerCallLocator(serverCallId);
                StartRecordingOptions startRecordingOptions = new StartRecordingOptions(callLocator);
                _ = await _callAutomation.GetCallRecording().StartAsync(startRecordingOptions);

                // Play message of start of recording
                await callingModule.PlayMessageThenWaitUntilItEndsAsync(_appointmentBookingConfig.AllPrompts.PlayRecordingStarted);

                while (true)
                {
                    // Top Level DTMF Menu, ask for which menu to be selected
                    string selectedTone = await callingModule.RecognizeTonesAsync(
                        originalTarget,
                        1,
                        1,
                        _appointmentBookingConfig.AllPrompts.MainMenu,
                        _appointmentBookingConfig.AllPrompts.Retry);

                    _logger.LogInformation($"Caller selected DTMF Tone[{selectedTone}]");

                    switch (selectedTone)
                    {
                        // Option 1:  Play Message and terminate the call.
                        case "1":
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_appointmentBookingConfig.AllPrompts.Choice1);
                            await callingModule.TerminateCallAsync();
                            return;

                        // Option 2: Play Message and terminate the call.
                        case "2":
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_appointmentBookingConfig.AllPrompts.Choice2);
                            await callingModule.TerminateCallAsync();
                            return;

                        // Option 3: Play Message and terminate the call.
                        case "3":
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_appointmentBookingConfig.AllPrompts.Choice3);
                            await callingModule.TerminateCallAsync();
                            return;

                        default:
                            // Wrong input!
                            // play message then retry this toplevel menu.
                            _logger.LogInformation($"Wrong Input! selectedTone[{selectedTone}]");
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_appointmentBookingConfig.AllPrompts.Retry);
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
            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_appointmentBookingConfig.AllPrompts.Goodbye);
            await callingModule.TerminateCallAsync();
            return;
        }
    }
}
