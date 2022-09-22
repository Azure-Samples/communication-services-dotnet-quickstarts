// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Communication.CallingServer.Sample.CallPlayAudio
{
    using Azure.Communication;
    using Azure.Communication.Identity;
    using Communication.CallingServer.Sample.CallPlayAudio.Ngrok;
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
                var ngrokPath = Constants.GetConfigSetting("NgrokExePath");

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
            CallPlayTerminate CallPlayTerminate = new CallPlayTerminate(callConfiguration);
            var callPlayTerminatePairs = Constants.GetConfigSetting("DestinationIdentities");
            if (callPlayTerminatePairs != null && callPlayTerminatePairs.Length > 0)
            {
                var identities = callPlayTerminatePairs.Split(';');
                var tasks = new List<Task>();
                foreach (var identity in identities)
                {
                    tasks.Add(Task.Run(async () => await new CallPlayTerminate(callConfiguration).Report(identity.Trim())));
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
            var connectionString = Constants.GetConfigSetting("Connectionstring");
            var sourcePhoneNumber = Constants.GetConfigSetting("SourcePhone");

            var sourceIdentity = await CreateUser(connectionString).ConfigureAwait(false);
            var audioFileName = await GenerateCustomAudioMessage().ConfigureAwait(false);

            return new CallConfiguration(connectionString, sourceIdentity.Id, sourcePhoneNumber, appBaseUrl, audioFileName);
        }

        private static async Task<string> GenerateCustomAudioMessage()
        {
            var key = ConfigurationManager.AppSettings["CognitiveServiceKey"];
            var region = ConfigurationManager.AppSettings["CognitiveServiceRegion"];
            var customMessage = ConfigurationManager.AppSettings["CustomMessage"];

            try
            {
                // audio file generation for custom message.
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(customMessage))
                {
                    var config = SpeechConfig.FromSubscription(key, region);
                    config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

                    var audioConfig = AudioConfig.FromWavFileOutput($"../../../audio/custom-message.wav");
                    var synthesizer = new SpeechSynthesizer(config, audioConfig);

                    await synthesizer.SpeakTextAsync(customMessage);

                    return "custom-message.wav";
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
    }
}
