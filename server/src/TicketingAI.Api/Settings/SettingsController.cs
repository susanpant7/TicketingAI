using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingAI.Api.Chat;
using TicketingAI.Api.Knowledge;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Settings;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class SettingsController(
    ISettingsService settingsService, DataCleanup dataCleanup, SavedAiModelService savedModels, AiModelLister modelLister,
    ApiKeyResolver keys, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<List<SettingDto>> GetAll() => await settingsService.GetAllAsync();

    [HttpPost]
    public async Task<ActionResult<SettingDto>> Create(CreateSettingRequest request)
    {
        var result = await settingsService.CreateAsync(request);
        return result.Saved is null ? Fail(result) : Created($"api/settings/{result.Saved.Setting.Key}", result.Saved.Setting);
    }

    /// <summary>
    /// Puts back every default setting. If that changes the embedding model, the stored knowledge is embedded again first, so this can
    /// take a while; a failure (the model or the key is not there) changes nothing.
    /// </summary>
    [HttpPost("restore-defaults")]
    public async Task<ActionResult<RestoreDefaultsResult>> RestoreDefaults(CancellationToken cancellationToken)
    {
        try
        {
            return new RestoreDefaultsResult(await settingsService.RestoreDefaultsAsync(cancellationToken));
        }
        catch (EmbeddingException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Deletes all data (tickets, users, knowledge, departments and settings), as on a fresh start.</summary>
    [HttpPost("clean-up")]
    public async Task<IActionResult> CleanUp()
    {
        await dataCleanup.CleanUpAsync();
        return NoContent();
    }

    /// <summary>The known services for the chat and the embedding models, and whether the key of each is set (never the key itself).</summary>
    [HttpGet("ai-providers")]
    public async Task<ActionResult<AiProvidersInfo>> GetAiProviders(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAiSettingsAsync(cancellationToken);
        // A service is ready when its key is set and, for Cloudflare, the account id in its address can be filled in.
        bool Ready(string baseUrl, bool needsKey, string keyName) =>
            !needsKey || (keys.IsSet(keyName) && !ApiKeyResolver.IsIncomplete(keys.ResolveAddress(baseUrl)));

        // On a server (Production) there is no Ollama on the same machine, so it is not offered.
        bool Offered(string baseUrl) => !environment.IsProduction() || !AiProviders.IsOllamaApi(baseUrl);

        return new AiProvidersInfo(
            AiProviders.All
                .Where(p => Offered(p.BaseUrl))
                .Select(p => new AiProviderDto(p.Name, p.BaseUrl, p.ExampleModel, p.NeedsApiKey, p.FreeTier, p.SignUpUrl, p.ApiKeyName,
                    Ready(p.BaseUrl, p.NeedsApiKey, p.ApiKeyName)))
                .ToList(),
            AiProviders.Embedding
                .Where(p => Offered(p.BaseUrl))
                .Select(p => new EmbeddingProviderDto(p.Name, p.BaseUrl, p.ExampleModel, p.NeedsApiKey,
                    p.FreeTier, p.SignUpUrl, p.ApiKeyName, Ready(p.BaseUrl, p.NeedsApiKey, p.ApiKeyName)))
                .ToList(),
            keys.Get(settings.ChatApiKeyName, settings.ChatBaseUrl) is not null,
            keys.Get(settings.EmbeddingApiKeyName, settings.EmbeddingBaseUrl) is not null,
            keys.EffectiveName(settings.ChatApiKeyName, settings.ChatBaseUrl),
            keys.EffectiveName(settings.EmbeddingApiKeyName, settings.EmbeddingBaseUrl));
    }

    /// <summary>The models of one kind (chat or embedding) that the service at a known address offers for the key with the given name.</summary>
    [HttpGet("available-models")]
    public async Task<ActionResult<AvailableAiModels>> GetAvailableModels(
        [FromQuery] string kind, [FromQuery] string baseUrl, [FromQuery] string? keyName, CancellationToken cancellationToken)
    {
        if (!SavedAiModelService.IsKind(kind))
            return Problem("The kind must be chat or embedding.", statusCode: StatusCodes.Status400BadRequest);

        try
        {
            return await modelLister.ListAsync(kind, baseUrl, keyName, cancellationToken);
        }
        catch (ChatModelException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Tests one model of a known service with a tiny request, and saves nothing. It answers with how long the model took when it works,
    /// and a problem with the service's own message when it does not (a retired name, a wrong key).
    /// </summary>
    [HttpPost("test-model/{kind}")]
    public async Task<ActionResult<TestModelResult>> TestModel(string kind, UseEmbeddingModelRequest request, CancellationToken cancellationToken)
    {
        if (!SavedAiModelService.IsKind(kind))
            return NotFound();

        try
        {
            await modelLister.EnsureKnownAsync(request.BaseUrl, cancellationToken);
            var clock = Stopwatch.StartNew();
            if (kind == SavedAiModel.Chat)
                await settingsService.TestChatModelAsync(new UseChatModelRequest(request.BaseUrl, request.Model, request.ApiKeyName), cancellationToken);
            else
                await settingsService.TestEmbeddingModelAsync(request, cancellationToken);
            return new TestModelResult(clock.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is ChatModelException or EmbeddingException)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Sets the Chat API base URL, the Chat model and the Chat API key name together, after the model answered a tiny test question.
    /// When it does not (a retired name, a wrong key), nothing is changed.
    /// </summary>
    [HttpPost("use-chat-model")]
    public async Task<ActionResult<UseModelResult>> UseChatModel(UseChatModelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await settingsService.UseChatModelAsync(request, cancellationToken);
            return result.Error == SettingError.None ? result : Problem(result.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ChatModelException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Converts the stored knowledge to a new embedding model, then saves the embedding settings. It can take a while, so the answer is sent
    /// as it goes, one JSON object per line: {"progress":{"done":16,"total":270}} lines, then {"result":{"reembedded":270}} or
    /// {"error":"..."}. When it fails (a wrong key, a model that is not there) nothing is changed.
    /// </summary>
    [HttpPost("use-embedding-model")]
    public async Task UseEmbeddingModel(UseEmbeddingModelRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "application/x-ndjson";
        async Task Send(object line)
        {
            await Response.WriteAsync(JsonSerializer.Serialize(line, Json) + "\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        try
        {
            var result = await settingsService.UseEmbeddingModelAsync(
                request, cancellationToken, (done, total) => Send(new { progress = new { done, total } }));
            await Send(result.Error == SettingError.None ? new { result = new { reembedded = result.Reembedded } } : new { error = result.Message });
        }
        catch (EmbeddingException ex)
        {
            await Send(new { error = ex.Message });
        }
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [HttpGet("saved-models/{kind}")]
    public async Task<ActionResult<List<SavedAiModelDto>>> GetSavedModels(string kind) =>
        SavedAiModelService.IsKind(kind) ? await savedModels.GetAllAsync(kind) : NotFound();

    [HttpPost("saved-models/{kind}")]
    public async Task<ActionResult<SavedAiModelDto>> AddSavedModel(string kind, SaveAiModelRequest request)
    {
        if (!SavedAiModelService.IsKind(kind))
            return NotFound();

        var result = await savedModels.CreateAsync(kind, request);
        return result.Model is null ? FailSavedModel(result) : result.Model;
    }

    [HttpPut("saved-models/{kind}/{id:int}")]
    public async Task<ActionResult<SavedAiModelDto>> UpdateSavedModel(string kind, int id, SaveAiModelRequest request)
    {
        if (!SavedAiModelService.IsKind(kind))
            return NotFound();

        var result = await savedModels.UpdateAsync(kind, id, request);
        return result.Model is null ? FailSavedModel(result) : result.Model;
    }

    [HttpDelete("saved-models/{kind}/{id:int}")]
    public async Task<IActionResult> DeleteSavedModel(string kind, int id) =>
        SavedAiModelService.IsKind(kind) && await savedModels.DeleteAsync(kind, id) ? NoContent() : NotFound();

    [HttpPut("{key}")]
    public async Task<ActionResult<SettingSaved>> Update(string key, UpdateSettingRequest request)
    {
        var result = await settingsService.UpdateAsync(key, request);
        return result.Saved is null ? Fail(result) : result.Saved;
    }

    private ActionResult FailSavedModel(SavedAiModelResult result) => result.Error switch
    {
        SavedAiModelError.NotFound => NotFound(),
        SavedAiModelError.NameTaken => Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
        _ => Problem(result.Message, statusCode: StatusCodes.Status400BadRequest)
    };

    private ActionResult Fail(SettingResult result) => result.Error switch
    {
        SettingError.NotFound => NotFound(),
        SettingError.KeyTaken => Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
        _ => Problem(result.Message, statusCode: StatusCodes.Status400BadRequest)
    };
}
