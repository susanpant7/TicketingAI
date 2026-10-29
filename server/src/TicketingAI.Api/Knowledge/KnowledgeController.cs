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
