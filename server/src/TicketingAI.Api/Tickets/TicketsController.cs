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

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TicketDto>> UpdateStatus(int id, UpdateTicketStatusRequest request)
    {
        var result = await ticketService.UpdateStatusAsync(id, User.GetEmployeeId(), request.Status);
        return result.Ticket is not null
            ? result.Ticket
            : result.Error == TicketError.NotFound
                ? NotFound()
                : Problem("Only the person a ticket is assigned to can change its status.", statusCode: StatusCodes.Status403Forbidden);
    }
}
