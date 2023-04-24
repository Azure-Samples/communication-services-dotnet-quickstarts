using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground.Services
{
    public class TopLevelMenuService : ITopLevelMenuService
    {
        private readonly ICallingService _callingService;
        private readonly PlaygroundConfig _playgroundConfig;

        public TopLevelMenuService(
            ICallingService callingService, PlaygroundConfig playgroundConfig)
        {
            _callingService = callingService;
            _playgroundConfig = playgroundConfig;
        }

        public async Task InvokeTopLevelMenu(CommunicationIdentifier target, string callConnectionId)
        {
            CallConnectionProperties callConnectionProperties = await _callingService.GetCallConnectionProperties(callConnectionId);

            // Top Level DTMF Menu
            CallMediaRecognizeDtmfOptions callMediaRecognizeDtmfOptions = new CallMediaRecognizeDtmfOptions(target, 1);
            callMediaRecognizeDtmfOptions.Prompt = new FileSource(_playgroundConfig.InitialPromptUri);
            callMediaRecognizeDtmfOptions.InterruptPrompt = true;

            IvrMenu mainMenu = new PlaygroundMainMenu(_callingService, _playgroundConfig);

            for (int i = 0; i < 3; i++)
            {
                await _callingService.RecognizeDtmfInput(callConnectionProperties,
                    async success =>
                    {
                        if (success.Tones.FirstOrDefault() == DtmfTone.One)
                        {
                            await mainMenu.OnPressOne(callConnectionProperties, target);
                        }

                        if (success.Tones.FirstOrDefault() == DtmfTone.Two)
                        {
                            await mainMenu.OnPressTwo(callConnectionProperties, target);
                        }

                        if (success.Tones.FirstOrDefault() == DtmfTone.Three)
                        {
                            await mainMenu.OnPressThree(callConnectionProperties, target);
                        }

                        if (success.Tones.FirstOrDefault() == DtmfTone.Four)
                        {
                            await mainMenu.OnPressFour(callConnectionProperties, target);
                        }

                        i = 0;

                    },
                    async failed =>
                    {
                        if (failed.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
                        {
                            // TODO: add retry logic
                        }
                    }, callMediaRecognizeDtmfOptions);
            }
        }
    }
}
