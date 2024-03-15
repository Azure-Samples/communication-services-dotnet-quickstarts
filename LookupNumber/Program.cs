using System;
using System.Threading.Tasks;
using Azure.Communication.PhoneNumbers;
internal class Program
{
    static async Task Main(string[] args)
    {
        // This code retrieves your connection string from an environment variable.
        string? connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");

        PhoneNumbersClient client = new PhoneNumbersClient(connectionString, new PhoneNumbersClientOptions(PhoneNumbersClientOptions.ServiceVersion.V2024_03_01_Preview));

        // Use the free number lookup functionality to get number formatting information
        OperatorInformationResult formattingResult = await client.SearchOperatorInformationAsync(new[] { "<target-phone-number>" });
        OperatorInformation formattingInfo = formattingResult.Values[0];
        Console.WriteLine($"{formattingInfo.PhoneNumber} is formatted {formattingInfo.InternationalFormat} internationally, and {formattingInfo.NationalFormat} nationally");

        // Use the paid number lookup functionality to get operator specific details
        // IMPORTANT NOTE: Invoking the method below will incur a charge to your account
        OperatorInformationResult searchResult = await client.SearchOperatorInformationAsync(new[] { "<target-phone-number>" }, new OperatorInformationOptions() { IncludeAdditionalOperatorDetails = true });
        OperatorInformation operatorInformation = searchResult.Values[0];
        Console.WriteLine($"{operatorInformation.PhoneNumber} is a {operatorInformation.NumberType ?? "unknown"} number, operated in {operatorInformation.IsoCountryCode} by {operatorInformation.OperatorDetails.Name ?? "an unknown operator"}");

    }
}
