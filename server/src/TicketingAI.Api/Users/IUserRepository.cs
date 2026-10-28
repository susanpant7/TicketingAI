using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Users;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}
