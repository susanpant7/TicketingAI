using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Departments;

public interface IDepartmentRepository
{
    Task<List<DepartmentDto>> GetAllAsync();
    Task<Department?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task<int> CountUsersAsync(int id);
    Task AddAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(Department department);
}
