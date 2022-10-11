namespace Calling.RecognizeDTMF
{
    using Azure.Communication;
    using Azure.Communication.Identity;
    using Calling.RecognizeDTMF.Ngrok;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.Owin.Hosting;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading.Tasks;

    class Program
    {
        static NgrokService ngrokService;
        const string url = "http://localhost:9007";

        enum AudioName
        {
            GeneralAudio,
            SalesAudio,
            MarketingAudio,
            CustomerCareAudio,
            InvalidAudio
        }

        static async Task Main(string[] args)
        {
            Logger.LogMessage(Logger.MessageType.INFORMATION, "Starting ACS Sample App");

            var ngrokUrl = await StartNgrokService();

            if (!string.IsNullOrEmpty(ngrokUrl))
            {
                using (WebApp.Start<Startup>(url))
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Server started at: {url}");

                    await Task.Run(async () =>
                    {
                        await RunSample(ngrokUrl).ConfigureAwait(false);
                    });

                    Logger.LogMessage(Logger.MessageType.INFORMATION, "Press any key to exit the sample");
                    Console.ReadLine();
                }
            }
            else
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "Failed to start Ngrok service");
                Console.ReadKey();
            }

            ngrokService?.Dispose();
        }

        /// <summary>
        /// Start ngrok server and fetch the assigned ngrok url
        /// </summary>
        /// <returns></returns>
        private static async Task<string> StartNgrokService()
        {
            try
            {
                var ngrokPath = GetConfigSetting("NgrokExePath");

                if (string.IsNullOrEmpty(ngrokPath))
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, "Ngrok path not provided");
                    return null;
                }

                Logger.LogMessage(Logger.MessageType.INFORMATION, "Starting Ngrok");
                ngrokService = new NgrokService(ngrokPath);

                Logger.LogMessage(Logger.MessageType.INFORMATION, "Fetching Ngrok Url");
                var ngrokUrl = await ngrokService.GetNgrokUrl();

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Ngrok Started with url: {ngrokUrl}");

                return ngrokUrl;
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Execute the sample
        /// </summary>
        /// <returns></returns>
        private static async Task RunSample(string appBaseUrl)
        {
            var callConfiguration = await InitiateConfiguration(appBaseUrl).ConfigureAwait(false);
            RecognizeDtmf outboundCallReminder = new RecognizeDtmf(callConfiguration);
            var outboundCallPairs = GetConfigSetting("DestinationIdentities");
            if (outboundCallPairs != null && outboundCallPairs.Length > 0)
            {
                var identities = outboundCallPairs.Split(';');
                var tasks = new List<Task>();
                foreach (var identity in identities)
                {
                    tasks.Add(Task.Run(async () => await new RecognizeDtmf(callConfiguration).Report(identity)));
                }

                await Task.WhenAll(tasks);
            }

            await DeleteUser(callConfiguration.ConnectionString, callConfiguration.SourceIdentity);
        }

        /// <summary>
        /// Fetch configurations from App Settings and create source identity
        /// </summary>
        /// <param name="appBaseUrl">The base url of the app.</param>
        /// <returns>The <c CallConfiguration object.</returns>
        private static async Task<CallConfiguration> InitiateConfiguration(string appBaseUrl)
        {
            var connectionString = GetConfigSetting("Connectionstring");
            var sourcePhoneNumber = GetConfigSetting("SourcePhone");

            var sourceIdentity = await CreateUser(connectionString).ConfigureAwait(false);
            var audioFileName = await GenerateCustomAudioMessage(AudioName.GeneralAudio).ConfigureAwait(false);
            var salesAudioFileName = await GenerateCustomAudioMessage(AudioName.SalesAudio).ConfigureAwait(false);
            var marketingAudioFileName = await GenerateCustomAudioMessage(AudioName.MarketingAudio).ConfigureAwait(false);
            var customerCareAudioFileName = await GenerateCustomAudioMessage(AudioName.CustomerCareAudio).ConfigureAwait(false);
            var invalidAudioFileName = await GenerateCustomAudioMessage(AudioName.InvalidAudio).ConfigureAwait(false);

            return new CallConfiguration(connectionString, sourceIdentity.Id, sourcePhoneNumber, appBaseUrl,
                audioFileName, salesAudioFileName, marketingAudioFileName, customerCareAudioFileName, invalidAudioFileName);
        }

        private static async Task<string> GenerateCustomAudioMessage(AudioName audioName)
        {
            var key = ConfigurationManager.AppSettings["CognitiveServiceKey"];
            var region = ConfigurationManager.AppSettings["CognitiveServiceRegion"];

            try
            {
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(region))
                {
                    var config = SpeechConfig.FromSubscription(key, region);
                    config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

                    if (audioName == AudioName.GeneralAudio)
                    {
                        var customMessage = ConfigurationManager.AppSettings["CustomMessage"];
                        var audioConfig = AudioConfig.FromWavFileOutput($"../../../audio/custom-message.wav");
                        var synthesizer = new SpeechSynthesizer(config, audioConfig);
                        await synthesizer.SpeakTextAsync(customMessage);
                        return "custom-message.wav";
                    }
                    else if (audioName == AudioName.SalesAudio)
                    {
                        var customMessage = ConfigurationManager.AppSettings["SalesCustomMessage"];
                        var audioConfig = AudioConfig.FromWavFileOutput($"../../../audio/sales-message.wav");
                        var synthesizer = new SpeechSynthesizer(config, audioConfig);
                        await synthesizer.SpeakTextAsync(customMessage);
                        return "sales-message.wav";
                    }
                    else if (audioName == AudioName.MarketingAudio)
                    {
                        var customMessage = ConfigurationManager.AppSettings["MarketingCustomMessage"];
                        var audioConfig = AudioConfig.FromWavFileOutput($"../../../audio/marketing-message.wav");
                        var synthesizer = new SpeechSynthesizer(config, audioConfig);
                        await synthesizer.SpeakTextAsync(customMessage);
                        return "marketing-message.wav";
                    }
                    else if (audioName == AudioName.CustomerCareAudio)
                    {
                        var customMessage = ConfigurationManager.AppSettings["CustomerCustomMessage"];
                        var audioConfig = AudioConfig.FromWavFileOutput($"../../../audio/customercare-message.wav");
                        var synthesizer = new SpeechSynthesizer(config, audioConfig);
                        await synthesizer.SpeakTextAsync(customMessage);
                        return "customercare-message.wav";
                    }
                    else if (audioName == AudioName.InvalidAudio)
                    {
                        var customMessage = ConfigurationManager.AppSettings["InvalidCustomMessage"];
                        var audioConfig = AudioConfig.FromWavFileOutput($"../../../audio/invalid-message.wav");
                        var synthesizer = new SpeechSynthesizer(config, audioConfig);
                        await synthesizer.SpeakTextAsync(customMessage);
                        return "invalid-message.wav";
                    }
                }
                else
                {
                    if (audioName == AudioName.GeneralAudio)
                    {
                        return "sample-message.wav";
                    }
                    else if (audioName == AudioName.SalesAudio)
                    {
                        return "sales.wav";
                    }
                    else if (audioName == AudioName.MarketingAudio)
                    {
                        return "marketing.wav";
                    }
                    else if (audioName == AudioName.CustomerCareAudio)
                    {
                        return "customercare.wav";
                    }
                    else if (audioName == AudioName.InvalidAudio)
                    {
                        return "invalid.wav";
                    }
                }

                return "sample-message.wav";
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Exception while generating text to speech, falling back to sample audio. Exception: {ex.Message}");
                return "sample-message.wav";
            }
        }

        /// <summary>
        /// Create new user
        /// </summary>
        /// <param name="connectionString">The connectionstring of Azure Communication Service resource.</param>
        /// <returns></returns>
        private static async Task<CommunicationUserIdentifier> CreateUser(string connectionString)
        {
            var client = new CommunicationIdentityClient(connectionString);
            var user = await client.CreateUserAsync().ConfigureAwait(false);
            return user.Value;
        }

        /// <summary>
        /// Delete the user
        /// </summary>
        /// <param name="connectionString">The connectionstring of Azure Communication Service resource.</param>
        /// <param name="source">The communication user.</param>
        /// <returns></returns>
        private static async Task DeleteUser(string connectionString, string source)
        {
            var client = new CommunicationIdentityClient(connectionString);
            await client.DeleteUserAsync(new CommunicationUserIdentifier(source)).ConfigureAwait(false);
        }

        /// <summary>
        /// get config key from env variable/app.config
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetConfigSetting(string key)
        {
            // try get from environment variable first
            string value = Environment.GetEnvironmentVariable(key);

            // try get from app.config value
            if (string.IsNullOrEmpty(value))
                value = ConfigurationManager.AppSettings[key];

            return value;
        }
    }
}
