using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Settings;

/// <summary>The chat models an admin saved, to switch to later with one click. Only the address and the model are saved, never a key.</summary>
public class SavedChatModelService(ISavedChatModelRepository models)
{
    private const int MaxNameLength = 100;
    private const int MaxUrlLength = 200;
    private const int MaxModelLength = 200;
    private const int MaxNotesLength = 500;

    public async Task<List<SavedChatModelDto>> GetAllAsync() => (await models.GetAllAsync()).Select(ToDto).ToList();

    public async Task<SavedChatModelResult> CreateAsync(SaveChatModelRequest request)
    {
        var (values, problem) = await CheckAsync(request, excludeId: null);
        if (problem is not null)
            return problem;

        var model = new SavedChatModel { Name = values!.Name, BaseUrl = values.BaseUrl, Model = values.Model, Notes = values.Notes };
        await models.AddAsync(model);
        return new SavedChatModelResult(ToDto(model));
    }

    public async Task<SavedChatModelResult> UpdateAsync(int id, SaveChatModelRequest request)
    {
        var model = await models.GetByIdAsync(id);
        if (model is null)
            return new SavedChatModelResult(null, SavedChatModelError.NotFound);

        var (values, problem) = await CheckAsync(request, excludeId: id);
        if (problem is not null)
            return problem;

        model.Name = values!.Name;
        model.BaseUrl = values.BaseUrl;
        model.Model = values.Model;
        model.Notes = values.Notes;
        await models.UpdateAsync(model);
        return new SavedChatModelResult(ToDto(model));
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var model = await models.GetByIdAsync(id);
        if (model is null)
            return false;

        await models.DeleteAsync(model);
        return true;
    }

    private async Task<(SaveChatModelRequest? Values, SavedChatModelResult? Problem)> CheckAsync(SaveChatModelRequest request, int? excludeId)
    {
        var name = request.Name.Trim();
        var baseUrl = request.BaseUrl.Trim().TrimEnd('/');
        var model = request.Model.Trim();
        var notes = request.Notes?.Trim() ?? "";

        if (name.Length == 0 || name.Length > MaxNameLength)
            return Invalid($"The name is required and can be up to {MaxNameLength} characters.");
        if (baseUrl.Length > MaxUrlLength || !(Uri.TryCreate(baseUrl, UriKind.Absolute, out var url) && url.Scheme is "http" or "https"))
            return Invalid($"The Chat API base URL must be a web address that starts with http:// or https://, up to {MaxUrlLength} characters.");
        if (model.Length == 0 || model.Length > MaxModelLength)
            return Invalid($"The model is required and can be up to {MaxModelLength} characters.");
        if (notes.Length > MaxNotesLength)
            return Invalid($"The notes can be up to {MaxNotesLength} characters.");
        if (await models.NameExistsAsync(name, excludeId))
            return (null, new SavedChatModelResult(null, SavedChatModelError.NameTaken, $"A chat model named \"{name}\" is already saved."));

        return (new SaveChatModelRequest(name, baseUrl, model, notes), null);
    }

    private static (SaveChatModelRequest?, SavedChatModelResult?) Invalid(string message) =>
        (null, new SavedChatModelResult(null, SavedChatModelError.Invalid, message));

    private static SavedChatModelDto ToDto(SavedChatModel m) => new(m.Id, m.Name, m.BaseUrl, m.Model, m.Notes);
}
