using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketingAI.Api.Settings;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class SettingsController(
    ISettingsService settingsService, DataCleanup dataCleanup, SavedChatModelService chatModels, IConfiguration configuration) : ControllerBase
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

    /// <summary>Deletes all data (tickets, users, knowledge, departments and settings), as on a fresh start.</summary>
    [HttpPost("clean-up")]
    public async Task<IActionResult> CleanUp()
    {
        await dataCleanup.CleanUpAsync();
        return NoContent();
    }

    /// <summary>The known services for the chat model, and whether the key for an online service is set (never the key itself).</summary>
    [HttpGet("ai-providers")]
    public ActionResult<AiProvidersInfo> GetAiProviders() =>
        new AiProvidersInfo(
            AiProviders.ApiKeyName, !string.IsNullOrWhiteSpace(configuration["ChatApi:ApiKey"]), AiProviders.All);

    [HttpGet("chat-models")]
    public async Task<List<SavedChatModelDto>> GetChatModels() => await chatModels.GetAllAsync();

    [HttpPost("chat-models")]
    public async Task<ActionResult<SavedChatModelDto>> AddChatModel(SaveChatModelRequest request)
    {
        var result = await chatModels.CreateAsync(request);
        return result.Model is null ? FailChatModel(result) : result.Model;
    }

    [HttpPut("chat-models/{id:int}")]
    public async Task<ActionResult<SavedChatModelDto>> UpdateChatModel(int id, SaveChatModelRequest request)
    {
        var result = await chatModels.UpdateAsync(id, request);
        return result.Model is null ? FailChatModel(result) : result.Model;
    }

    [HttpDelete("chat-models/{id:int}")]
    public async Task<IActionResult> DeleteChatModel(int id) => await chatModels.DeleteAsync(id) ? NoContent() : NotFound();

    private ActionResult FailChatModel(SavedChatModelResult result) => result.Error switch
    {
        SavedChatModelError.NotFound => NotFound(),
        SavedChatModelError.NameTaken => Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
        _ => Problem(result.Message, statusCode: StatusCodes.Status400BadRequest)
    };

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
