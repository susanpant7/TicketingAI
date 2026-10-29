using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingAI.Api.Common;

namespace TicketingAI.Api.Tickets;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TicketsController(ITicketService ticketService) : ControllerBase
{
    [HttpGet]
    public async Task<List<TicketDto>> GetAll() => await ticketService.GetAllAsync(User.GetEmployeeId());
}
