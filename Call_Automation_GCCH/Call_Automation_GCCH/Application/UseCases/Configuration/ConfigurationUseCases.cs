using Call_Automation_GCCH.Core.Common;
using Call_Automation_GCCH.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Call_Automation_GCCH.Application.UseCases.Configuration;

public class GetConfigurationUseCase
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<GetConfigurationUseCase> _logger;

    public GetConfigurationUseCase(IConfigurationService configurationService, ILogger<GetConfigurationUseCase> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    public async Task<Result<ApplicationConfiguration>> ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Getting application configuration");
            var result = await _configurationService.GetConfigurationAsync();
            return Result<ApplicationConfiguration>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration");
            return Result<ApplicationConfiguration>.Failure($"Failed to get configuration: {ex.Message}");
        }
    }
}

public class UpdateConfigurationUseCase
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<UpdateConfigurationUseCase> _logger;

    public UpdateConfigurationUseCase(IConfigurationService configurationService, ILogger<UpdateConfigurationUseCase> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    public async Task<Result<ApplicationConfiguration>> ExecuteAsync(ConfigurationUpdate update)
    {
        try
        {
            _logger.LogInformation("Updating application configuration");

            if (update == null)
                return Result<ApplicationConfiguration>.Failure("Update data is required");

            var result = await _configurationService.UpdateConfigurationAsync(update);
            return Result<ApplicationConfiguration>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return Result<ApplicationConfiguration>.Failure($"Failed to update configuration: {ex.Message}");
        }
    }
}
