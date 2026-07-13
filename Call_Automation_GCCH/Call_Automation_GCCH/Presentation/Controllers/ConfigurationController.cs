using Call_Automation_GCCH.Application.UseCases.Configuration;
using Call_Automation_GCCH.Core.Interfaces;
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
}
