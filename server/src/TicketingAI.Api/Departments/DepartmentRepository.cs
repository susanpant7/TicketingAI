using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Departments;

public class DepartmentRepository(AppDbContext db) : IDepartmentRepository
{
    public Task<List<DepartmentDto>> GetAllAsync() =>
        db.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Description, d.Contact, d.Users.Count))
            .ToListAsync();

    public Task<Department?> GetByIdAsync(int id) => db.Departments.FirstOrDefaultAsync(d => d.Id == id);

    public Task<bool> ExistsAsync(int id) => db.Departments.AnyAsync(d => d.Id == id);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null) =>
        db.Departments.AnyAsync(d => d.Name.ToLower() == name.ToLower() && d.Id != excludeId);

    public Task<int> CountUsersAsync(int id) => db.Users.CountAsync(u => u.DepartmentId == id);

    public async Task AddAsync(Department department)
    {
        db.Departments.Add(department);
        await db.SaveChangesAsync();
    }

    public Task UpdateAsync(Department department) => db.SaveChangesAsync();

    public async Task DeleteAsync(Department department)
    {
        db.Departments.Remove(department);
        await db.SaveChangesAsync();
    }
}
