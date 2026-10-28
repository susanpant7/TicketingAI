using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Users;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<List<User>> GetAllAsync() =>
        db.Users.Include(u => u.Department).OrderBy(u => u.Id).ToListAsync();

    public Task<User?> GetByIdAsync(int id) =>
        db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public Task<bool> UsernameExistsAsync(string username) =>
        db.Users.AnyAsync(u => u.Username == username);

    public Task<bool> EmailExistsAsync(string email, int? excludeUserId = null) =>
        db.Users.AnyAsync(u => u.Email == email && u.Id != excludeUserId);

    public async Task AddAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public Task UpdateAsync(User user) => db.SaveChangesAsync();

    public async Task DeleteAsync(User user)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }
}
