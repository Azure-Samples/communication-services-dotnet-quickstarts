using Azure.Communication.CallAutomation;
namespace CogSvcIvrSamples
{
    public static class ExtensionMethods
    {
        public static TextSource ToTextPlaySource(this string payload, string voiceName = "en-US-NancyNeural", string playSourceId = null, string locale = "en-US", string gender = "female")
        {
            return new TextSource(payload)
            {
                PlaySourceId = playSourceId,
                VoiceName = voiceName,
                SourceLocale = locale,
                VoiceGender = gender
            };
        }

        public static SsmlSource ToSsmlPlaySource(this string payload, string voiceName = "en-US-NancyNeural", string playSourceId = null, string locale = "en-US", string expression = "default")
        {
            var ssml = $"<speak xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"http://www.w3.org/2001/mstts\" xmlns:emo=\"http://www.w3.org/2009/10/emotionml\" version=\"1.0\" xml:lang=\"{locale}\">" +
                            $"<voice name=\"{voiceName}\">" +
                                $"<mstts:express-as style=\"{expression}\">{payload}</mstts:express-as><s />" +
                            "</voice>" +
                       "</speak>";
            return new SsmlSource(ssml)
            {
                PlaySourceId = playSourceId
            };
        }

        public static string ToBase64(this string payload)
        {
            var textBytes = System.Text.Encoding.UTF8.GetBytes(payload);
            return Convert.ToBase64String(textBytes);
        }

        public static string FromBase64(this string base64Payload)
        {
            var base64Bytes = System.Convert.FromBase64String(base64Payload);
            return System.Text.Encoding.UTF8.GetString(base64Bytes);
        }
    }
}
