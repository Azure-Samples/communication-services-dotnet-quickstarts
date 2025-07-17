using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Communication.PhoneNumbers;

namespace PhoneNumbers
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Get a connection string to our Azure Communication resource.
            var connectionString = "<connection_string>";
            var client = new PhoneNumbersClient(connectionString);

            // Search phone numbers
            var capabilities = new PhoneNumberCapabilities(calling:PhoneNumberCapabilityType.None, sms:PhoneNumberCapabilityType.Outbound);
            var searchOptions = new PhoneNumberSearchOptions { AreaCode = "833", Quantity = 1 };

            var searchOperation = await client.StartSearchAvailablePhoneNumbersAsync("US", PhoneNumberType.TollFree, PhoneNumberAssignmentType.Application, capabilities, searchOptions);
            await searchOperation.WaitForCompletionAsync();

            var phoneNumber = searchOperation.Value.PhoneNumbers.First();

            // Purchase searched phone numbers
            var purchaseOperation = await client.StartPurchasePhoneNumbersAsync(searchOperation.Value.SearchId);
            await purchaseOperation.WaitForCompletionResponseAsync();

            // Get purchased phone number(s)
            var getPhoneNumberResponse = await client.GetPurchasedPhoneNumberAsync(phoneNumber);
            Console.WriteLine($"Successfully purchased phone number: {getPhoneNumberResponse.Value.PhoneNumber}, country code: {getPhoneNumberResponse.Value.CountryCode}");

            var purchasedPhoneNumbers = client.GetPurchasedPhoneNumbersAsync();
            await foreach (var purchasedPhoneNumber in purchasedPhoneNumbers)
            {
                Console.WriteLine($"Phone number: {purchasedPhoneNumber.PhoneNumber}, country code: {purchasedPhoneNumber.CountryCode}");
            }

            // Update capabilities
            var updateCapabilitiesOperation = await client.StartUpdateCapabilitiesAsync(phoneNumber, calling: PhoneNumberCapabilityType.Outbound, sms: PhoneNumberCapabilityType.InboundOutbound);
            await updateCapabilitiesOperation.WaitForCompletionAsync();
            Console.WriteLine("Phone number updated successfully.");

            // Release phone number
            var releaseOperation = await client.StartReleasePhoneNumberAsync(phoneNumber);
            await releaseOperation.WaitForCompletionResponseAsync();
            Console.WriteLine("Successfully release phone number.");
        }
    }
}
