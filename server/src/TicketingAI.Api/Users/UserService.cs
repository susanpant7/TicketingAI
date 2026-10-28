using TicketingAI.Api.Departments;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Users;

public class UserService(IUserRepository users, IDepartmentRepository departments) : IUserService
{
    public async Task<List<UserDto>> GetAllAsync() =>
        (await users.GetAllAsync()).Select(ToDto).ToList();

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await users.GetByIdAsync(id);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserResult> CreateAsync(CreateUserRequest request)
    {
        if (!await departments.ExistsAsync(request.DepartmentId))
            return new UserResult(null, UserError.InvalidDepartment);
        if (await users.UsernameExistsAsync(request.Username))
            return new UserResult(null, UserError.UsernameTaken);
        if (await users.EmailExistsAsync(request.Email))
            return new UserResult(null, UserError.EmailTaken);

        var user = new User
        {
            Username = request.Username,
            FullName = request.FullName,
            Email = request.Email,
            Password = request.Password,
            DepartmentId = request.DepartmentId
        };
        await users.AddAsync(user);

        return new UserResult(await GetByIdAsync(user.Id));
    }

    public async Task<UserResult> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await users.GetByIdAsync(id);
        if (user is null)
            return new UserResult(null, UserError.NotFound);
        if (!await departments.ExistsAsync(request.DepartmentId))
            return new UserResult(null, UserError.InvalidDepartment);
        if (await users.EmailExistsAsync(request.Email, id))
            return new UserResult(null, UserError.EmailTaken);

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.DepartmentId = request.DepartmentId;
        if (!string.IsNullOrEmpty(request.Password))
            user.Password = request.Password;
        await users.UpdateAsync(user);

        return new UserResult(await GetByIdAsync(id));
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await users.GetByIdAsync(id);
        if (user is null)
            return false;

        await users.DeleteAsync(user);
        return true;
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Username, user.FullName, user.Email, user.DepartmentId, user.Department.Name);
}
