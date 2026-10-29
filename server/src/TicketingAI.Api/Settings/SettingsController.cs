using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketingAI.Api.Settings;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    public async Task<List<SettingDto>> GetAll() => await settingsService.GetAllAsync();

    [HttpPost]
    public async Task<ActionResult<SettingDto>> Create(CreateSettingRequest request)
    {
        var result = await settingsService.CreateAsync(request);
        return result.Saved is null ? Fail(result) : Created($"api/settings/{result.Saved.Setting.Key}", result.Saved.Setting);
    }

    [HttpPost("restore-defaults")]
    public async Task<ActionResult<RestoreDefaultsResult>> RestoreDefaults() =>
        new RestoreDefaultsResult(await settingsService.RestoreDefaultsAsync());

    [HttpPut("{key}")]
    public async Task<ActionResult<SettingSaved>> Update(string key, UpdateSettingRequest request)
    {
        var result = await settingsService.UpdateAsync(key, request);
        return result.Saved is null ? Fail(result) : result.Saved;
    }

    private ActionResult Fail(SettingResult result) => result.Error switch
    {
        SettingError.NotFound => NotFound(),
        SettingError.KeyTaken => Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
        _ => Problem(result.Message, statusCode: StatusCodes.Status400BadRequest)
    };
}
