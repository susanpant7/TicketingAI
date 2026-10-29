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

    [HttpPost("ingest")]
    public async Task<ActionResult<IngestResult>> Ingest(CancellationToken cancellationToken)
    {
        try
        {
            return await knowledgeService.IngestAsync(cancellationToken);
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

    [HttpDelete("chunks")]
    public async Task<ActionResult<object>> DeleteAll() => new { Deleted = await knowledgeService.DeleteAllAsync() };
}
