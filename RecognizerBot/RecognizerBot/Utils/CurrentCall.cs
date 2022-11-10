using Azure.Communication.CallAutomation;
using IncomingCallRouting.Models;

namespace IncomingCallRouting.Utils
{
    public static class CurrentCall
    {
        public static CallConnection CallConnection;

        public static CallConnectionProperties CallConnectionProperties;

        public static AudioStream AudioStream;
    }
}
