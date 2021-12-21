
#load "Logger.csx"

using System;
using Azure.Communication.Sms;
using System.Threading.Tasks;
using Azure;

class SendSMS
{
    private SmsClient smsClient;
    public SendSMS()
    {
        string connectionString = Environment.GetEnvironmentVariable("Connectionstring");
        smsClient = new SmsClient(connectionString);
    }

    public async Task SendOneToOneSms(string sourcePhoneNumber, string targetPhoneNumber, string message)
    {
        try
        {
            Response<SmsSendResult> sendResult = await smsClient.SendAsync(
                from: sourcePhoneNumber,
                to: targetPhoneNumber,
                message: message
            );
            Logger.LogMessage(Logger.MessageType.INFORMATION, $"Sms sent successfully with SMS-id: {sendResult.Value.MessageId}");
        }
        catch (Exception ex)
        {
            Logger.LogMessage(Logger.MessageType.ERROR, $"Send message failed unexpectedly, reason: {ex.Message}");
        }
    }
}