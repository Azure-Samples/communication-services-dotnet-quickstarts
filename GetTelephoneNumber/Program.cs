using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Communication.PhoneNumbers;

namespace GetTelephoneNumber
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "<connection_string>";
            var client = new PhoneNumbersClient(connectionString);

            // Search phone number
            var capabilities = new PhoneNumberCapabilities(calling: PhoneNumberCapabilityType.None, sms: PhoneNumberCapabilityType.Outbound);
            var searchOptions = new PhoneNumberSearchOptions { AreaCode = "833", Quantity = 1 };

            var searchOperation = await client.StartSearchAvailablePhoneNumbersAsync("US", PhoneNumberType.TollFree, PhoneNumberAssignmentType.Application, capabilities, searchOptions);
            await searchOperation.WaitForCompletionAsync();

            var phoneNumber = searchOperation.Value.PhoneNumbers.First();

            // Purchase searched phone number
            var purchaseOperation = await client.StartPurchasePhoneNumbersAsync(searchOperation.Value.SearchId);
            await purchaseOperation.WaitForCompletionResponseAsync();

            // Get purchased phone number
            var getPhoneNumberResponse = await client.GetPurchasedPhoneNumberAsync(phoneNumber);
            Console.WriteLine($"Phone number: {getPhoneNumberResponse.Value.PhoneNumber}, country code: {getPhoneNumberResponse.Value.CountryCode}");

            // Get all purchased phone numbers
            var purchasedPhoneNumbers = client.GetPurchasedPhoneNumbersAsync();
            await foreach (var purchasedPhoneNumber in purchasedPhoneNumbers)
            {
                Console.WriteLine($"Phone number: {purchasedPhoneNumber.PhoneNumber}, country code: {purchasedPhoneNumber.CountryCode}");
            }

            // Update capabilities of the purchased number
            var updateCapabilitiesOperation = await client.StartUpdateCapabilitiesAsync(phoneNumber, calling: PhoneNumberCapabilityType.Outbound, sms: PhoneNumberCapabilityType.InboundOutbound);
            await updateCapabilitiesOperation.WaitForCompletionAsync();

            // Release the purchased phone number
            var releaseOperation = await client.StartReleasePhoneNumberAsync(phoneNumber);
            await releaseOperation.WaitForCompletionResponseAsync();
        }
    }
}
