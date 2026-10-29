namespace TicketingAI.Api.Departments;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync();
    Task<DepartmentResult> CreateAsync(DepartmentRequest request);
    Task<DepartmentResult> UpdateAsync(int id, DepartmentRequest request);

    /// <summary>Deletes a department. A department that still has users cannot be deleted.</summary>
    Task<DepartmentResult> DeleteAsync(int id);
}
