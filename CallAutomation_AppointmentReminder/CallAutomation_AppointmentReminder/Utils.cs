using Azure.Communication.CallAutomation;
using Microsoft.Extensions.Options;

namespace CallAutomation_AppointmentReminder
{
    public static class Utils
    {
       public static PlaySource GetAudioForTone(DtmfTone toneDetected, IOptions<CallConfiguration> callConfiguration)
        {
            FileSource playSource;

            if (toneDetected.Equals(DtmfTone.One))
            {
                playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentConfirmedAudio));
            }
            else if (toneDetected.Equals(DtmfTone.Two))
            {
                playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentCancelledAudio));
            }
            else if (toneDetected.Equals(DtmfTone.Three))
            {
                playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AgentAudio));
            }
            else // Invalid Dtmf tone
            {
                playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.InvalidInputAudio));
            }

            return playSource;
        }
    }
}
