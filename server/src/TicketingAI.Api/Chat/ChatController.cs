using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingAI.Api.Knowledge;

namespace TicketingAI.Api.Chat;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController(IChatService chatService, ILogger<ChatController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Streams the reply as newline-delimited JSON: "token" events with text as it is written,
    /// then one "done" event with the final answer and sources, or an "error" event.
    /// </summary>
    [HttpPost]
    public async Task Ask(ChatRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "application/x-ndjson";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // tells proxies such as nginx not to hold the stream back

        try
        {
            await foreach (var chatEvent in chatService.AskAsync(request, cancellationToken))
                await WriteAsync(chatEvent, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser went away (tab closed or page left). Stopping here also stops the model.
        }
        catch (Exception ex) when (ex is ChatModelException or EmbeddingException)
        {
            await WriteAsync(ChatStreamEvent.Error(ex.Message), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat request failed");
            await WriteAsync(ChatStreamEvent.Error("Something went wrong while answering. Please try again."), CancellationToken.None);
        }
    }

    private async Task WriteAsync(ChatStreamEvent chatEvent, CancellationToken cancellationToken)
    {
        await Response.WriteAsync(JsonSerializer.Serialize(chatEvent, JsonOptions) + "\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
