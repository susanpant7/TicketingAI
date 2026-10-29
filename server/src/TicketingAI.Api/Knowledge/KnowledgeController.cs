using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketingAI.Api.Knowledge;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class KnowledgeController(IKnowledgeService knowledgeService) : ControllerBase
{
    [HttpGet("chunks")]
    public async Task<List<KnowledgeChunkDto>> GetChunks() => await knowledgeService.GetChunksAsync();

    [HttpGet("search")]
    public async Task<ActionResult<List<KnowledgeSearchResult>>> Search(
        [FromQuery] string q, [FromQuery] int k = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length > 500)
            return Problem("Provide a question of 1 to 500 characters in 'q'.", statusCode: StatusCodes.Status400BadRequest);

        try
        {
            return await knowledgeService.SearchAsync(q.Trim(), Math.Clamp(k, 1, 20), cancellationToken);
        }
        catch (EmbeddingException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("reload")]
    public async Task<ActionResult<ReloadResult>> Reload(CancellationToken cancellationToken)
    {
        try
        {
            return await knowledgeService.ReloadAsync(cancellationToken);
        }
        catch (ReloadInProgressException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (EmbeddingException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }
}
