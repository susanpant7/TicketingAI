using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Departments;

public class DepartmentRepository(AppDbContext db) : IDepartmentRepository
{
    public Task<List<Department>> GetAllAsync() =>
        db.Departments.OrderBy(d => d.Name).ToListAsync();

    public Task<bool> ExistsAsync(int id) => db.Departments.AnyAsync(d => d.Id == id);
}
