using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingAI.Api.Common;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Chat;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Ask(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await chatService.AskAsync(request, User.GetEmployeeId(), cancellationToken);
        }
        catch (Exception ex) when (ex is ChatModelException or EmbeddingException)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
