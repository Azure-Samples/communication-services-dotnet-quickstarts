using Azure.Communication.CallAutomation;
using Azure.Communication;

namespace CallAutomation.Scenarios
{
    public class PlayAudio
    {
        public enum AudioText
        {
            Identification,
            Authentication,
            MainMenu,
            MagentaHome,
            MagentaMobile,
            MagentaTV,
            InvalidAudio,
            UnIdentifiedUser
        }

        public static async Task PlayAudioOperation(DtmfTone toneReceived, IConfiguration configuration, 
            CallConnection callConnection)
        {
            var audioPlayOptions = new PlayOptions() { OperationContext = "SimpleIVR", Loop = false };

            if (toneReceived == DtmfTone.One || toneReceived.ToString().Equals(AudioText.MagentaHome.ToString()))
            {
                await PlayAudioToAll(audioPlayOptions, AudioText.MagentaHome, configuration, callConnection);
            }
            else if (toneReceived == DtmfTone.Two || toneReceived.ToString().Equals(AudioText.MagentaMobile.ToString()))
            {
                await PlayAudioToAll(audioPlayOptions, AudioText.MagentaMobile, configuration, callConnection);
            }
            else if (toneReceived == DtmfTone.Three || toneReceived.ToString().Equals(AudioText.MagentaTV.ToString()))
            {
                await PlayAudioToAll(audioPlayOptions, AudioText.MagentaTV, configuration, callConnection);
            }
            else if (toneReceived == DtmfTone.Five || toneReceived.ToString().Equals("Cancel"))
            {
                // Hangup for everyone
                _ = await callConnection.HangUpAsync(true);
            }
            else
            {
                await PlayAudioToAll(audioPlayOptions, AudioText.InvalidAudio, configuration, callConnection);
            }
        }

        public static async Task PlayAudioToAll(PlayOptions audioPlayOptions, AudioText audioText, 
            IConfiguration configuration, CallConnection callConnection)
        {
            //you can provide SourceLocale and VoiceGender as one option for playing audio
            TextSource playSource = new TextSource(configuration[audioText.ToString()]) { 
                SourceLocale= "en-US",
                VoiceGender = GenderType.Female,
            };

            _ = await callConnection.GetCallMedia().PlayToAllAsync(playSource, audioPlayOptions);
        }
    }
}
