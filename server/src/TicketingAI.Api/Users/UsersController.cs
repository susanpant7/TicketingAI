using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingAI.Api.Common;

namespace TicketingAI.Api.Users;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<List<UserDto>> GetAll() => await userService.GetAllAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null ? NotFound() : user;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var result = await userService.CreateAsync(request);
        return result.User is null
            ? Fail(result.Error)
            : CreatedAtAction(nameof(GetById), new { id = result.User.Id }, result.User);
    }

    [HttpPost("load-defaults")]
    public async Task<ActionResult<LoadDefaultsResult>> LoadDefaults(LoadDefaultUsersRequest request)
    {
        var result = await userService.LoadDefaultsAsync(request.Password);
        return result is null
            ? Problem("There are no departments yet. Create the departments first, because every user belongs to one.", statusCode: StatusCodes.Status400BadRequest)
            : result;
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request)
    {
        var result = await userService.UpdateAsync(id, request);
        return result.User is null ? Fail(result.Error) : result.User;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await userService.DeleteAsync(id) ? NoContent() : NotFound();

    private ActionResult Fail(UserError error) => error switch
    {
        UserError.NotFound => NotFound(),
        UserError.UsernameTaken => Problem("Username already exists.", statusCode: StatusCodes.Status409Conflict),
        UserError.EmailTaken => Problem("Email already exists.", statusCode: StatusCodes.Status409Conflict),
        _ => Problem("Department does not exist.", statusCode: StatusCodes.Status400BadRequest)
    };
}
