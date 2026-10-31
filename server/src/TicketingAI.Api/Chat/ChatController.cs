using System.Text.Json;
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

    /// <summary>
    /// The same answer, sent while it is written: one JSON object per line. {"draft": "..."} lines carry the reply so far,
    /// and the last line is {"reply": {...}} with the final answer, or {"error": "..."}.
    /// </summary>
    [HttpPost("stream")]
    public async Task Stream(ChatRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "application/x-ndjson";
        async Task Send(object line)
        {
            await Response.WriteAsync(JsonSerializer.Serialize(line, Json) + "\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        try
        {
            var reply = await chatService.AskAsync(request, User.GetEmployeeId(), cancellationToken, draft => Send(new { draft }));
            await Send(new { reply });
        }
        catch (Exception ex) when (ex is ChatModelException or EmbeddingException)
        {
            await Send(new { error = ex.Message });
        }
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
}
