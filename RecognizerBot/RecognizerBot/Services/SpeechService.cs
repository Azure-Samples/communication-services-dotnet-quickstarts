using Azure;
using Azure.Communication.CallAutomation;
using Azure.Storage.Blobs;
using IncomingCallRouting.Utils;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using IncomingCallRouting.Models;
using IncomingCallRouting.Nuance.Models;
using Azure.Core;
using Microsoft.CognitiveServices.Speech.Intent;
using IncomingCallRouting.Interfaces;

namespace IncomingCallRouting.Services
{
    public class SpeechService : ISpeechService
    {
        private readonly IConfiguration _configuration;
        private readonly IStorageService _storageService;

        public SpeechService(IConfiguration configuration, IStorageService storageService)
        {
            _configuration = configuration;
            _storageService = storageService;
        }

        public async Task PlayIntent(string intent)
        {
            try
            {
                var speechAudioResult = await TextToSpeech(intent);

                var playSource = new FileSource(speechAudioResult.AudioFileUri);

                var response = await CurrentCall.CallConnection.GetCallMedia()
                    .PlayToAllAsync(playSource, new PlayOptions { OperationContext = Guid.NewGuid().ToString() });

                Logger.LogMessage(Logger.MessageType.INFORMATION, $"PlayAsync response --> {response.Status}, Id: {response.ClientRequestId}");
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Logger.MessageType.ERROR, $"Failure occurred while playing audio on the call. Exception: {ex.Message}");
            }
        }

        public async Task<SpeechAudioResult> TextToSpeech(string text)
        {
            var speechConfig = SpeechConfig.FromSubscription(_configuration["SpeechKey"], "eastus");

            // The language of the voice that speaks.
            speechConfig.SpeechSynthesisVoiceName = "en-US-JennyNeural";

            var fileName = $"{text}.wav";
            var (fileExists, uri) = await _storageService.Exists(fileName);

            var result = new SpeechAudioResult { AudioFileUri = uri };

            if (!fileExists)
            {
                using (var audioConfig = AudioConfig.FromWavFileOutput(fileName))
                {
                    using var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

                    // Synthesize to the default speaker and save to file
                    result.SpeechSynthesisResult = await speechSynthesizer.SpeakTextAsync(text);
                    result.CancellationDetails = await OutputSpeechSynthesisResult(result.SpeechSynthesisResult, text);
                }

                if (result.CancellationDetails == null)
                {
                    result.AudioFileUri = await _storageService.UploadTo(fileName, fileName);
                }
            }
            else
            {
                Logger.LogMessage(Logger.MessageType.INFORMATION, $"Audio file with {fileName} already exists.");
            }

            return result;
        }

        public async Task<Intent> ExtractIntent(InterpretResponse response)
        {
            return new Intent
            {
                Value = response?.Result?.Interpretation?.SingleIntentInterpretation?.Intent,
                Confidence = response?.Result?.Interpretation?.SingleIntentInterpretation?.Confidence
            };
        }

        public async Task<SpeechSynthesisCancellationDetails?> OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
        {
            switch (speechSynthesisResult.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    Logger.LogMessage(Logger.MessageType.INFORMATION, $"Speech synthesized for text: [{text}]");
                    break;
                case ResultReason.Canceled:
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                    Logger.LogMessage(Logger.MessageType.ERROR, $"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Logger.LogMessage(Logger.MessageType.ERROR, $"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Logger.LogMessage(Logger.MessageType.ERROR, $"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                        Logger.LogMessage(Logger.MessageType.ERROR, $"CANCELED: Did you set the speech resource key and region values?");
                    }

                    return cancellation;
            }

            return null;
        }
    }
}
