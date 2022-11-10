using Azure.Communication.CallAutomation;
using IncomingCallRouting.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using IncomingCallRouting.Models;
using IncomingCallRouting.Nuance.Models;
using IncomingCallRouting.Interfaces;

namespace IncomingCallRouting.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SpeechController : ControllerBase
    {
        private readonly ISpeechService _speechService;
        private readonly IRegonizeService _recognizeService;

        public SpeechController(ISpeechService speechService, IRegonizeService recognizeService, ILogger<SpeechController> logger)
        {
            _speechService = speechService;
            _recognizeService = recognizeService;
            Logger.SetLoggerInstance(logger);
        }

        /// Receive intent from nuance mix and play speech
        [HttpPost("playIntent")]
        public async Task<IActionResult> PlayIntent([FromBody] InterpretResponse request)
        {
            var intent = await _speechService.ExtractIntent(request);

            await _speechService.PlayIntent(intent.Value);

            return Ok();
        }

        /// Save text to audio
        [HttpPost("tts")]
        public async Task<IActionResult> TextToSpeech([FromBody] SpeechText request)
        {
            var speechAudioResult = await _speechService.TextToSpeech(request.Text);

            if (speechAudioResult.CancellationDetails != null)
            {
                return BadRequest(speechAudioResult.CancellationDetails);
            }

            return Ok(new { speechAudioResult.AudioFileUri, Duration = speechAudioResult.SpeechSynthesisResult?.AudioDuration, speechAudioResult.SpeechSynthesisResult?.Properties });
        }

        /// Recognize From audio file
        [HttpPost("recognize")]
        public async Task<IActionResult> RecognizeIntent([FromBody] RecognizeRequest request)
        {
            await _recognizeService.RecognizeIntentFromFile(request.FilePath);

            return Ok();
        }

        /// Recognize From audio stream
        [HttpPost("recognizeStream")]
        public async Task<IActionResult> RecognizeIntentFromStream()
        {
            await _recognizeService.RecognizeIntentFromStream();

            return Ok();
        }

    }
}
