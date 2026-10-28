using Microsoft.Extensions.Options;
using TicketingAI.Api.Users;

namespace TicketingAI.Api.Auth;

public class AuthService(
    IOptions<AdminOptions> adminOptions,
    IUserRepository userRepository,
    ITokenService tokenService) : IAuthService
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

    private readonly AdminOptions _admin = adminOptions.Value;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        if (request.Username == _admin.Username && request.Password == _admin.Password)
            return CreateResponse(_admin.Username, _admin.Username, AdminRole);

        var user = await userRepository.GetByUsernameAsync(request.Username);
        return user?.Password == request.Password
            ? CreateResponse(user.Id.ToString(), user.Username, UserRole)
            : null;
    }

    private LoginResponse CreateResponse(string subject, string username, string role) =>
        new(username, role, tokenService.CreateToken(subject, username, role));
}
