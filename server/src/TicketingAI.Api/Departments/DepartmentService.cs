using TicketingAI.Api.Common;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Departments;

public class DepartmentService(IDepartmentRepository departments) : IDepartmentService
{
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 500;
    private const int MaxContactLength = 300;

    public Task<List<DepartmentDto>> GetAllAsync() => departments.GetAllAsync();

    public async Task<DepartmentResult> CreateAsync(DepartmentRequest request)
    {
        var problem = await CheckAsync(request, excludeId: null);
        if (problem is not null)
            return problem;

        var department = new Department
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? "",
            Contact = request.Contact?.Trim() ?? ""
        };
        await departments.AddAsync(department);

        return new DepartmentResult(ToDto(department, userCount: 0));
    }

    public async Task<DepartmentResult> UpdateAsync(int id, DepartmentRequest request)
    {
        var department = await departments.GetByIdAsync(id);
        if (department is null)
            return new DepartmentResult(Error: DepartmentError.NotFound);

        var problem = await CheckAsync(request, excludeId: id);
        if (problem is not null)
            return problem;

        department.Name = request.Name.Trim();
        department.Description = request.Description?.Trim() ?? "";
        department.Contact = request.Contact?.Trim() ?? "";
        await departments.UpdateAsync(department);

        return new DepartmentResult(ToDto(department, await departments.CountUsersAsync(id)));
    }

    public async Task<LoadDefaultsResult> LoadDefaultsAsync()
    {
        var added = 0;
        foreach (var defaults in DefaultDepartments.All)
        {
            if (await departments.NameExistsAsync(defaults.Name))
                continue;

            await departments.AddAsync(new Department
            {
                Name = defaults.Name,
                Description = defaults.Description ?? "",
                Contact = defaults.Contact ?? ""
            });
            added++;
        }

        return new LoadDefaultsResult(added, DefaultDepartments.All.Count - added);
    }

    public async Task<DepartmentResult> DeleteAsync(int id)
    {
        var department = await departments.GetByIdAsync(id);
        if (department is null)
            return new DepartmentResult(Error: DepartmentError.NotFound);

        var userCount = await departments.CountUsersAsync(id);
        if (userCount > 0)
        {
            var users = userCount == 1 ? "1 user" : $"{userCount} users";
            return new DepartmentResult(
                Error: DepartmentError.HasUsers,
                Message: $"This department has {users}, so it cannot be deleted. Move or delete its users first.");
        }

        await departments.DeleteAsync(department);
        return new DepartmentResult();
    }

    private async Task<DepartmentResult?> CheckAsync(DepartmentRequest request, int? excludeId)
    {
        var name = request.Name.Trim();
        if (name.Length == 0 || name.Length > MaxNameLength)
            return Invalid($"The name is required and can be up to {MaxNameLength} characters.");
        if ((request.Description?.Trim().Length ?? 0) > MaxDescriptionLength)
            return Invalid($"The description can be up to {MaxDescriptionLength} characters.");
        if ((request.Contact?.Trim().Length ?? 0) > MaxContactLength)
            return Invalid($"The contact can be up to {MaxContactLength} characters.");

        if (await departments.NameExistsAsync(name, excludeId))
            return new DepartmentResult(Error: DepartmentError.NameTaken, Message: "A department with this name already exists.");

        return null;
    }

    private static DepartmentResult Invalid(string message) => new(Error: DepartmentError.Invalid, Message: message);

    private static DepartmentDto ToDto(Department d, int userCount) => new(d.Id, d.Name, d.Description, d.Contact, userCount);
}
