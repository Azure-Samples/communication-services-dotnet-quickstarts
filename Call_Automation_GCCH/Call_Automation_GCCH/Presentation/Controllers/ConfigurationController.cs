using Call_Automation_GCCH.Application.UseCases.Configuration;
using Call_Automation_GCCH.Core.Interfaces;
using Call_Automation_GCCH.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Call_Automation_GCCH.Presentation.Controllers;

[ApiController]
[Route("api/v2/configuration")]
[Produces("application/json")]
[Tags("Configuration Management (Clean Architecture)")]
public class ConfigurationController : ControllerBase
{
    private readonly GetConfigurationUseCase _getConfigurationUseCase;
    private readonly UpdateConfigurationUseCase _updateConfigurationUseCase;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        GetConfigurationUseCase getConfigurationUseCase,
        UpdateConfigurationUseCase updateConfigurationUseCase,
        ILogger<ConfigurationController> logger)
    {
        _getConfigurationUseCase = getConfigurationUseCase;
        _updateConfigurationUseCase = updateConfigurationUseCase;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApplicationConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConfiguration()
    {
        _logger.LogInformation("GET /api/v2/configuration");

        var result = await _getConfigurationUseCase.ExecuteAsync();

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.ErrorMessage });
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApplicationConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateConfiguration([FromBody] ConfigurationUpdate update)
    {
        _logger.LogInformation("PUT /api/v2/configuration");

        var result = await _updateConfigurationUseCase.ExecuteAsync(update);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.ErrorMessage });
    }

    /// <summary>
    /// Gets all logs collected since browser/session start.
    /// Logs persist until browser reset.
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(LogsResponse), StatusCodes.Status200OK)]
    public IActionResult GetLogs()
    {
        _logger.LogInformation("GET /api/v2/configuration/logs - Retrieving all logs");

        var logs = LogCollector.GetAll();
        var logCount = LogCollector.GetLogCount();

        return Ok(new LogsResponse
        {
            TotalLogCount = logCount,
            DisplayedLogCount = logs.Count,
            Logs = logs,
            Timestamp = DateTime.UtcNow,
            Message = "Logs persist until browser reset"
        });
    }

    /// <summary>
    /// Gets recent logs from the last N minutes.
    /// </summary>
    [HttpGet("logs/recent")]
    [ProducesResponseType(typeof(LogsResponse), StatusCodes.Status200OK)]
    public IActionResult GetRecentLogs([FromQuery] int minutes = 5)
    {
        _logger.LogInformation("GET /api/v2/configuration/logs/recent - Last {Minutes} minutes", minutes);

        var logs = LogCollector.GetRecent(minutes);
        var logCount = LogCollector.GetLogCount();

        return Ok(new LogsResponse
        {
            TotalLogCount = logCount,
            DisplayedLogCount = logs.Count,
            Logs = logs,
            Timestamp = DateTime.UtcNow,
            Message = $"Logs from last {minutes} minutes"
        });
    }

    /// <summary>
    /// Clears all collected logs.
    /// </summary>
    [HttpPost("logs/clear")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult ClearLogs()
    {
        _logger.LogInformation("POST /api/v2/configuration/logs/clear - Clearing all logs");

        LogCollector.Clear();

        return Ok(new { message = "Logs cleared successfully", timestamp = DateTime.UtcNow });
    }
}

/// <summary>
/// Response model for logs endpoint.
/// </summary>
public class LogsResponse
{
    public int TotalLogCount { get; set; }
    public int DisplayedLogCount { get; set; }
    public List<string> Logs { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}
