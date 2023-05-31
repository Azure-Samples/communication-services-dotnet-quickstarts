using Azure.Communication.PhoneNumbers;
using Azure.Communication.Sms;

public class LookupPhoneNumber
{
    static async Task Main(string[] args)
    {
        // This code retrieves your connection string from an environment variable.
        string? connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");

        // Initialize clients
        PhoneNumbersClient client = new PhoneNumbersClient(connectionString, new PhoneNumbersClientOptions(PhoneNumbersClientOptions.ServiceVersion.V2023_05_01_Preview));
        SmsClient smsClient = new SmsClient(connectionString);

        // Set up the from/to numbers for this sample
        string recipientNumber = "<to-phone-number>";
        string acsNumber = "<from-phone-number>";

        try
        {
            // Lookup recipient phone number
            OperatorInformationResult searchResult = await client.SearchOperatorInformationAsync(new[] { recipientNumber });
            OperatorInformation operatorInformation = searchResult.Results[0];

            Console.WriteLine($"{operatorInformation.PhoneNumber} is a {operatorInformation.NumberType ?? "unknown"} number, operated by {operatorInformation.OperatorDetails.Name ?? "an unknown operator"}");

            // Send an SMS if the recipient is a mobile number in the US
            if (operatorInformation.OperatorDetails.MobileCountryCode == "310")
            {
                SmsSendResult sendResult = smsClient.Send(
                    from: acsNumber,
                    to: operatorInformation.PhoneNumber,
                    message: "Hello World from the Number Lookup sample!"
                );
                Console.WriteLine($"Sms id: {sendResult.MessageId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
