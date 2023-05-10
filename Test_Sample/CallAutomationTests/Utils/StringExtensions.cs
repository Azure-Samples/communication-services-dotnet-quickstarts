namespace CallAutomation.Scenarios.Utils
{
    public static class StringExtensions
    {
        public static string? SanitizePhoneNumber(this string? phoneNumber)
        {
            if (phoneNumber == null) return null;

            phoneNumber = phoneNumber.Trim().Split('+')[^1].Split(':')[^1];
            phoneNumber = string.Join(string.Empty, phoneNumber.Split(' '));
            phoneNumber = string.Join(string.Empty, phoneNumber.Split('-'));

            return phoneNumber;
        }
    }
}
