using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketingAI.Api.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var response = await authService.LoginAsync(request);
        return response is null ? Unauthorized() : response;
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me() =>
        new { Username = User.Identity!.Name, Role = User.FindFirst("role")!.Value };
}
