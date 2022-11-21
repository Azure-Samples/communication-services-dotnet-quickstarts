using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using Microsoft.Extensions.Configuration;
using RecognizerBot.Interfaces;
using RecognizerBot.Utils;

namespace RecognizerBot.Services
{
    public class RecognizeService : IRegonizeService
    {
        private readonly IConfiguration _configuration;
        private readonly ISpeechService _speechService;

        public RecognizeService(IConfiguration configuration, ISpeechService speechService)
        {
            _configuration = configuration;
            _speechService = speechService;
        }

        /**
         * <summary>
         * Continuous intent recognition using file input.
         * </summary>
         */
        public async Task RecognizeIntentFromFile(string filePath)
        {
            var speechConfig = SpeechConfig.FromSubscription(_configuration["LuisKey"], "eastus");

            // Creates an intent recognizer using file as audio input.
            using var audioInput = AudioConfig.FromWavFileInput(filePath);
            using var recognizer = new IntentRecognizer(speechConfig, audioInput);

            // The TaskCompletionSource to stop recognition.
            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Creates a Language Understanding model using the app id, and adds specific intents from your model
            var model = LanguageUnderstandingModel.FromAppId(_configuration["LuisAppId"]);
            // recognizer.AddAllIntents(model);
            recognizer.AddIntent(model, "RestaurantReservation.Reserve", "make reservation");
            recognizer.AddIntent(model, "RestaurantReservation.DeleteReservation", "delete reservation");
            recognizer.AddIntent(model, "RestaurantReservation.FindReservationEntry", "find reservation");

            // Subscribes to events.
            recognizer.Recognizing += (s, e) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"RECOGNIZING: Text={e.Result.Text}");
            };

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedIntent)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"RECOGNIZED: Text={e.Result.Text}");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"    Intent Id: {e.Result.IntentId}.");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"    Language Understanding JSON: {e.Result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult)}.");
                }
                else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"RECOGNIZED: Text={e.Result.Text}");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, "    Intent not recognized.");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, "NOMATCH: Speech could not be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"CANCELED: ErrorCode={e.ErrorCode}");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, "CANCELED: Did you update the subscription info?");
                }

                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStarted += (s, e) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "\n    Session started event.");
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "\n    Session stopped event.");
                Logger.LogMessage(Logger.MessageType.INFORMATION, "\nStop recognition.");
                stopRecognition.TrySetResult(0);
            };

            // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            // Waits for completion.
            Task.WaitAny(stopRecognition.Task);

            // Stops recognition.
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }

        /**
         * <summary>
         * Continuous intent recognition using audio stream input.
         * </summary>
         */
        public async Task RecognizeIntentFromStream(bool playback = false)
        {
            var speechConfig = SpeechConfig.FromSubscription(_configuration["LuisKey"], "eastus");

            // Creates an intent recognizer using file as audio input.
            // Initialize with the format required by the Speech service
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);

            // Configure speech SDK to work with the audio stream in right format.
            using var audioConfig = AudioConfig.FromStreamInput(CurrentCall.AudioStream, audioFormat);
            // using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
            using var recognizer = new IntentRecognizer(speechConfig, audioConfig);

            // The TaskCompletionSource to stop recognition.
            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Creates a Language Understanding model using the app id, and adds specific intents from your model
            var model = LanguageUnderstandingModel.FromAppId(_configuration["LuisAppId"]);
            // recognizer.AddAllIntents(model);
            recognizer.AddIntent(model, "RestaurantReservation.Reserve", "make reservation");
            recognizer.AddIntent(model, "RestaurantReservation.DeleteReservation", "delete reservation");
            recognizer.AddIntent(model, "RestaurantReservation.FindReservationEntry", "find reservation");

            // Subscribes to events.
            recognizer.Recognizing += (s, e) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"RECOGNIZING: Text={e.Result.Text}");
            };

            recognizer.Recognized += async (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedIntent)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"RECOGNIZED: Text={e.Result.Text}");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"    Intent Id: {e.Result.IntentId}.");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"    Language Understanding JSON: {e.Result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult)}.");
                    
                    if (playback)
                    {
                        await _speechService.PlayIntent(e.Result.IntentId);
                    }
                }
                else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"RECOGNIZED: Text={e.Result.Text}");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, "    Intent not recognized.");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, "NOMATCH: Speech could not be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"CANCELED: ErrorCode={e.ErrorCode}");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Logger.LogMessage(Logger.MessageType.INFORMATION, "CANCELED: Did you update the subscription info?");
                }

                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStarted += (s, e) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "\n    Session started event.");
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, "\n    Session stopped event.");
                Logger.LogMessage(Logger.MessageType.INFORMATION, "\nStop recognition.");
                stopRecognition.TrySetResult(0);
            };

            // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            // Waits for completion.
            Task.WaitAny(stopRecognition.Task);

            // Stops recognition.
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        }
    }
}
