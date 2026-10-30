using TicketingAI.Api.Common;
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

    public async Task<LoadDefaultsResult?> LoadDefaultsAsync(string password)
    {
        var all = (await departments.GetAllAsync()).OrderBy(d => d.Id).ToList();
        if (all.Count == 0)
            return null;

        var added = 0;
        for (var i = 0; i < DefaultUsers.All.Count; i++)
        {
            var (first, last) = DefaultUsers.All[i];
            var username = first.ToLowerInvariant();
            var email = $"{username}@ticket.com";
            if (await users.UsernameExistsAsync(username) || await users.EmailExistsAsync(email))
                continue;

            await users.AddAsync(new User
            {
                Username = username,
                FullName = $"{first} {last}",
                Email = email,
                Password = password,
                // Block i * departments / users: with 15 users and 3 departments, 5 users each, in order.
                DepartmentId = all[i * all.Count / DefaultUsers.All.Count].Id
            });
            added++;
        }

        return new LoadDefaultsResult(added, DefaultUsers.All.Count - added);
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Username, user.FullName, user.Email, user.DepartmentId, user.Department.Name);
}
