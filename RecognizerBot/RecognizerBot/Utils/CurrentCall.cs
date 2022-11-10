using Azure.Communication.CallAutomation;
using RecognizerBot.Models;

namespace RecognizerBot.Utils
{
    public static class CurrentCall
    {
        public static CallConnection CallConnection;

        public static CallConnectionProperties CallConnectionProperties;

        public static AudioStream AudioStream;
    }
}
