using TicketingAI.Api.Common;

namespace TicketingAI.Api.Users;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserResult> CreateAsync(CreateUserRequest request);
    Task<UserResult> UpdateAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Adds the default users (all with the given password), divided into equal blocks over the existing departments.
    /// A user whose username or email is already taken is skipped. Returns null if there are no departments.
    /// </summary>
    Task<LoadDefaultsResult?> LoadDefaultsAsync(string password);
}
