namespace CallAutomationOpenAI
{
    public static class ExtensionMethods
    {
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
