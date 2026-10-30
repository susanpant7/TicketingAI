using TicketingAI.Api.Common;

namespace TicketingAI.Api.Departments;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync();
    Task<DepartmentResult> CreateAsync(DepartmentRequest request);
    Task<DepartmentResult> UpdateAsync(int id, DepartmentRequest request);

    /// <summary>Adds the default departments (IT, HR, Finance) that do not exist yet. Existing ones are left as they are.</summary>
    Task<LoadDefaultsResult> LoadDefaultsAsync();

    /// <summary>Deletes a department. A department that still has users cannot be deleted.</summary>
    Task<DepartmentResult> DeleteAsync(int id);
}
