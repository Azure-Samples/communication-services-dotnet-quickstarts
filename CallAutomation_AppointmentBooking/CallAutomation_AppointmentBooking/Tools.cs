namespace CallAutomation_AppointmentBooking
{
    public static class Tools
    {
        public static string FormatPhoneNumbers(string phoneNumber)
        {
            // calculate E.164 format phonenumber.
            // +1 xxx-xxx-xxxx
            // update this tools as your need.
            if (phoneNumber == null)
            {
                throw new ArgumentNullException(nameof(phoneNumber));
            }

            // Remove all non-digit characters from the phone number
            phoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (phoneNumber.Length == 10)
            {
                return "+1" + phoneNumber;
            }
            else if (phoneNumber.Length == 11 && phoneNumber.StartsWith("1"))
            {
                return "+" + phoneNumber;
            }
            else if (phoneNumber.Length == 12 && phoneNumber.StartsWith("+1"))
            {
                return phoneNumber;
            }
            else
            {
                throw new ArgumentException("Invalid phone number");
            }
        }

    }
}
