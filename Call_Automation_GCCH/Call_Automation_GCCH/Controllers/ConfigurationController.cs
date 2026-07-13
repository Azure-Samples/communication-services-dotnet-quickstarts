using Call_Automation_GCCH.Models;
using Call_Automation_GCCH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Call_Automation_GCCH.Controllers
{
    [ApiController]
    [Route("api/configuration")]
    [Produces("application/json")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ICallAutomationService _service;
        private readonly AcsCommunicationSettings _config;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            ICallAutomationService service,
            IOptions<AcsCommunicationSettings> configOptions,
            ILogger<ConfigurationController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sets the runtime configuration. This must be called before making any call operations.
        /// Only non-empty values are applied — omit or leave blank to keep the current value.
        /// </summary>
        /// <param name="acsConnectionString">ACS connection string (endpoint=...;accesskey=...)</param>
        /// <param name="pmaEndpoint">PMA endpoint URL (leave empty to skip PMA)</param>
        /// <param name="callbackUriHost">Public base URL for callbacks (e.g. https://myapp.devtunnels.ms)</param>
        /// <param name="acsPhoneNumber">ACS phone number in E.164 format (e.g. +18001234567)</param>
        [HttpPost("set")]
        [Tags("1. Configuration")]
        public IActionResult SetConfiguration(
            string? acsConnectionString = null,
            string? pmaEndpoint = null,
            string? callbackUriHost = null,
            string? acsPhoneNumber = null)
        {
            bool clientUpdated = false;

            // ACS connection string + PMA
            if (!string.IsNullOrWhiteSpace(acsConnectionString))
            {
                _config.AcsConnectionString = acsConnectionString;
                _service.UpdateClient(acsConnectionString, pmaEndpoint ?? _service.GetCurrentPmaEndpoint());
                clientUpdated = true;
                _logger.LogInformation("ACS connection string updated. PmaEndpoint={PmaEndpoint}", pmaEndpoint ?? "(unchanged)");
            }
            else if (!string.IsNullOrWhiteSpace(pmaEndpoint) && !string.IsNullOrWhiteSpace(_config.AcsConnectionString))
            {
                _service.UpdateClient(_config.AcsConnectionString, pmaEndpoint);
                clientUpdated = true;
                _logger.LogInformation("PMA endpoint updated to {PmaEndpoint}", pmaEndpoint);
            }

            // Callback URI
            if (!string.IsNullOrWhiteSpace(callbackUriHost))
            {
                _config.CallbackUriHost = callbackUriHost.TrimEnd('/');
                _logger.LogInformation("CallbackUriHost updated to {CallbackUriHost}", _config.CallbackUriHost);
            }

            // Phone number
            if (!string.IsNullOrWhiteSpace(acsPhoneNumber))
            {
                _config.AcsPhoneNumber = acsPhoneNumber;
                _logger.LogInformation("AcsPhoneNumber updated to {AcsPhoneNumber}", _config.AcsPhoneNumber);
            }

            return Ok(new
            {
                Message = "Configuration updated",
                ClientRecreated = clientUpdated,
                Current = BuildCurrentConfig()
            });
        }

        /// <summary>
        /// Returns the current runtime configuration. Connection strings are masked.
        /// </summary>
        [HttpGet("current")]
        [Tags("1. Configuration")]
        public IActionResult GetCurrentConfiguration()
        {
            return Ok(BuildCurrentConfig());
        }

        /// <summary>
        /// Returns the current runtime configuration for UI (unmasked phone/callback, masked connection string)
        /// </summary>
        [HttpGet("get")]
        [Tags("1. Configuration")]
        public IActionResult GetConfiguration()
        {
            return Ok(new
            {
                AcsConnectionString = string.IsNullOrEmpty(_config.AcsConnectionString) ? null : "***configured***",
                AcsPhoneNumber = _config.AcsPhoneNumber,
                CallbackUriHost = _config.CallbackUriHost,
                PmaEndpoint = _service.GetCurrentPmaEndpoint(),
                AudioFileUrl = _config.AudioFileUrl,
                IsConfigured = !string.IsNullOrEmpty(_config.AcsConnectionString) && !string.IsNullOrEmpty(_config.AcsPhoneNumber)
            });
        }

        private object BuildCurrentConfig()
        {
            return new
            {
                AcsConnectionString = Mask(_config.AcsConnectionString),
                AcsPhoneNumber = _config.AcsPhoneNumber ?? "(not set)",
                CallbackUriHost = _config.CallbackUriHost ?? "(not set)",
                PmaEndpoint = string.IsNullOrEmpty(_service.GetCurrentPmaEndpoint()) ? "(not set)" : _service.GetCurrentPmaEndpoint(),
                IsClientInitialized = !string.IsNullOrEmpty(_config.AcsConnectionString)
            };
        }

        private static string Mask(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "(not set)";
            return value.Length > 30 ? value.Substring(0, 30) + "..." : "***";
        }
    }
}
