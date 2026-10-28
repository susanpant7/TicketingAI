using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Departments;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync();
    Task<bool> ExistsAsync(int id);
}
