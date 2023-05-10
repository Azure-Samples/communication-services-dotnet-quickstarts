using Azure.Communication.CallAutomation;
using Azure.Communication;
using static CallAutomation.Scenarios.PlayAudio;
using System.Globalization;

namespace CallAutomation.Scenarios
{
    public class Recognize
    {

        public enum RecognizeFor
        {
            Identification,
            Autentication
        }
        public static async Task StartRecognizingDtmf(string callerId,
            IConfiguration configuration,
            CallConnection callConnection,
            RecognizeFor recognizeFor)
        {
            CallMediaRecognizeDtmfOptions? recognizeOptions = null;

            // Start recognize prompt
            switch (recognizeFor)
            {
                case RecognizeFor.Identification:
                    recognizeOptions = new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 10);
                    recognizeOptions.Prompt = new TextSource(configuration[PlayAudio.AudioText.Identification.ToString()])
                    {
                        SourceLocale = "en-US",
                        VoiceGender = GenderType.Female,
                    };
                    recognizeOptions.OperationContext = "Identification";

                    break;
                case RecognizeFor.Autentication:
                    recognizeOptions = new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 4);
                    recognizeOptions.Prompt = new TextSource(configuration[PlayAudio.AudioText.Authentication.ToString()])
                    {
                        SourceLocale = "en-US",
                        VoiceGender = GenderType.Female,
                    };
                    recognizeOptions.OperationContext = "Authentication";

                    break;
            }

            if (recognizeOptions != null)
            {
                recognizeOptions.InterruptPrompt = true;
                recognizeOptions.InterToneTimeout = TimeSpan.FromSeconds(10);
                recognizeOptions.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
            }
            _ = await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions);
        }

        public static async Task StartRecognizingChoice(string callerId, IConfiguration configuration, CallConnection callConnection)
        {
            var choices = new List<RecognizeChoice>
            {
                new RecognizeChoice(AudioText.MagentaHome.ToString(), new List<string> { "Home", "Magenta Home", "First", "One"})
                {
                    Tone = DtmfTone.One
                },
                new RecognizeChoice(AudioText.MagentaMobile.ToString(), new List<string> { "Mobile", "Magenta Mobile", "Second", "Two"})
                {
                    Tone = DtmfTone.Two
                },
                new RecognizeChoice(AudioText.MagentaTV.ToString(), new List<string> { "TV", "Magenta TV", "Third", "Three"})
                {
                    Tone = DtmfTone.Three
                }
            };

            // Start recognize prompt - play audio and recognize 1-digit DTMF input
            var recognizeOptions =
                new CallMediaRecognizeChoiceOptions(CommunicationIdentifier.FromRawId(callerId), choices)
                {
                    InterruptPrompt = true,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                    Prompt = new TextSource(configuration[PlayAudio.AudioText.MainMenu.ToString()])
                    {
                        SourceLocale = "en-US",
                        VoiceGender = GenderType.Female,
                    },
                    OperationContext = "MainMenu"
                };
            _ = await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions);
        }

        public static string CombineDtmfTones(IReadOnlyList<DtmfTone> dtmfTones)
        {
            var combined = "";

            //only digit for now...
            foreach (DtmfTone dtmf in dtmfTones)
            {
                switch (dtmf.ToString().ToLower(CultureInfo.InvariantCulture))
                {
                    case "zero":
                        combined += "0";
                        break;
                    case "one":
                        combined += "1";
                        break;
                    case "two":
                        combined += "2";
                        break;
                    case "three":
                        combined += "3";
                        break;
                    case "four":
                        combined += "4";
                        break;
                    case "five":
                        combined += "5";
                        break;
                    case "six":
                        combined += "6";
                        break;
                    case "seven":
                        combined += "7";
                        break;
                    case "eight":
                        combined += "8";
                        break;
                    case "nine":
                        combined += "9";
                        break;
                    default:
                        break;
                }
            }
            return combined;
        }
    }
}
