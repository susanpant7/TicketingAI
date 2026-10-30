using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Users;

public record LoadDefaultUsersRequest([Required] string Password);
