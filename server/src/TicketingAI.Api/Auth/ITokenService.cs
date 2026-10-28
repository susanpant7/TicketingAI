namespace TicketingAI.Api.Auth;

public interface ITokenService
{
    string CreateToken(string subject, string username, string role);
}
