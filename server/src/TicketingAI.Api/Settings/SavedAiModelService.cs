using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Settings;

/// <summary>
/// The chat and embedding models an admin saved, to switch to later with one click. Only the address, the model, the NAME of the key's
/// environment variable and (for embeddings) the prefixes are saved, never a key.
/// </summary>
public class SavedAiModelService(ISavedAiModelRepository models, ApiKeyResolver keys)
{
    private const int MaxNameLength = 100;
    private const int MaxUrlLength = 200;
    private const int MaxModelLength = 200;
    private const int MaxPrefixLength = 100;
    private const int MaxNotesLength = 500;

    public static bool IsKind(string kind) => kind is SavedAiModel.Chat or SavedAiModel.Embedding;

    public async Task<List<SavedAiModelDto>> GetAllAsync(string kind) => (await models.GetAllAsync(kind)).Select(ToDto).ToList();

    public async Task<SavedAiModelResult> CreateAsync(string kind, SaveAiModelRequest request)
    {
        var (values, problem) = await CheckAsync(kind, request, excludeId: null);
        if (problem is not null)
            return problem;

        var model = new SavedAiModel
        {
            Kind = kind, Name = values!.Name, BaseUrl = values.BaseUrl, Model = values.Model, ApiKeyName = values.ApiKeyName ?? "",
            QueryPrefix = values.QueryPrefix ?? "", DocumentPrefix = values.DocumentPrefix ?? "", Notes = values.Notes ?? ""
        };
        await models.AddAsync(model);
        return new SavedAiModelResult(ToDto(model));
    }

    public async Task<SavedAiModelResult> UpdateAsync(string kind, int id, SaveAiModelRequest request)
    {
        var model = await models.GetByIdAsync(kind, id);
        if (model is null)
            return new SavedAiModelResult(null, SavedAiModelError.NotFound);

        var (values, problem) = await CheckAsync(kind, request, excludeId: id);
        if (problem is not null)
            return problem;

        model.Name = values!.Name;
        model.BaseUrl = values.BaseUrl;
        model.Model = values.Model;
        model.ApiKeyName = values.ApiKeyName ?? "";
        model.QueryPrefix = values.QueryPrefix ?? "";
        model.DocumentPrefix = values.DocumentPrefix ?? "";
        model.Notes = values.Notes ?? "";
        await models.UpdateAsync(model);
        return new SavedAiModelResult(ToDto(model));
    }

    public async Task<bool> DeleteAsync(string kind, int id)
    {
        var model = await models.GetByIdAsync(kind, id);
        if (model is null)
            return false;

        await models.DeleteAsync(model);
        return true;
    }

    private async Task<(SaveAiModelRequest? Values, SavedAiModelResult? Problem)> CheckAsync(string kind, SaveAiModelRequest request, int? excludeId)
    {
        var name = request.Name.Trim();
        var baseUrl = request.BaseUrl.Trim().TrimEnd('/');
        var model = request.Model.Trim();
        var keyName = request.ApiKeyName?.Trim() ?? "";
        var notes = request.Notes?.Trim() ?? "";
        // The prefixes belong to an embedding model; a chat model has none.
        var queryPrefix = kind == SavedAiModel.Embedding ? request.QueryPrefix?.Trim() ?? "" : "";
        var documentPrefix = kind == SavedAiModel.Embedding ? request.DocumentPrefix?.Trim() ?? "" : "";

        if (name.Length == 0 || name.Length > MaxNameLength)
            return Invalid($"The name is required and can be up to {MaxNameLength} characters.");
        if (baseUrl.Length > MaxUrlLength || !(Uri.TryCreate(baseUrl, UriKind.Absolute, out var url) && url.Scheme is "http" or "https"))
            return Invalid($"The base URL must be a web address that starts with http:// or https://, up to {MaxUrlLength} characters.");
        if (model.Length == 0 || model.Length > MaxModelLength)
            return Invalid($"The model is required and can be up to {MaxModelLength} characters.");
        if (keyName.Length > 0 && !ApiKeyResolver.IsValidName(keyName))
            return Invalid("The key name must be the name of an environment variable that ends in API_KEY, for example GROQ_API_KEY.");
        if (queryPrefix.Length > MaxPrefixLength || documentPrefix.Length > MaxPrefixLength)
            return Invalid($"A prefix can be up to {MaxPrefixLength} characters.");
        if (notes.Length > MaxNotesLength)
            return Invalid($"The notes can be up to {MaxNotesLength} characters.");
        if (await models.NameExistsAsync(kind, name, excludeId))
            return (null, new SavedAiModelResult(null, SavedAiModelError.NameTaken, $"A model named \"{name}\" is already saved."));

        return (new SaveAiModelRequest(name, baseUrl, model, keyName, queryPrefix, documentPrefix, notes), null);
    }

    private static (SaveAiModelRequest?, SavedAiModelResult?) Invalid(string message) =>
        (null, new SavedAiModelResult(null, SavedAiModelError.Invalid, message));

    private SavedAiModelDto ToDto(SavedAiModel m) =>
        new(m.Id, m.Kind, m.Name, m.BaseUrl, m.Model, m.ApiKeyName, keys.Get(m.ApiKeyName, m.BaseUrl) is not null, m.QueryPrefix, m.DocumentPrefix, m.Notes);
}
