using Microsoft.Extensions.Options;

namespace TicketingAI.Api.Auth;

public class AuthService(
    IOptions<AdminOptions> adminOptions,
    IUserRepository userRepository) : IAuthService
{
    private readonly AdminOptions _admin = adminOptions.Value;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        if (request.Username == _admin.Username && request.Password == _admin.Password)
            return new LoginResponse(_admin.Username, "Admin");

        var user = await userRepository.GetByUsernameAsync(request.Username);
        return user?.Password == request.Password ? new LoginResponse(user.Username, "User") : null;
    }
}
