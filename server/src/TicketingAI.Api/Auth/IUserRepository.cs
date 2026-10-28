using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Auth;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
}
