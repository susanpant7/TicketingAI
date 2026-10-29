using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketingAI.Api.Knowledge;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class KnowledgeController(IKnowledgeService knowledgeService) : ControllerBase
{
    private const long MaxFileBytes = 1_000_000;

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

    [HttpDelete("department/{departmentId:int}")]
    public async Task<ActionResult<DeleteKnowledgeResult>> DeleteDepartment(int departmentId)
    {
        try
        {
            return new DeleteKnowledgeResult(await knowledgeService.DeleteDepartmentKnowledgeAsync(departmentId));
        }
        catch (UploadDepartmentNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<List<UploadFileResult>>> Upload(
        [FromForm] int departmentId, [FromForm] bool flush, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files.Count == 0)
            return Problem("Select at least one file.", statusCode: StatusCodes.Status400BadRequest);

        var uploaded = new List<UploadedFile>();
        var rejected = new List<UploadFileResult>();
        foreach (var file in files)
        {
            if (file.Length > MaxFileBytes)
            {
                rejected.Add(new UploadFileResult(file.FileName, 0, "The file is larger than 1 MB."));
                continue;
            }

            using var reader = new StreamReader(file.OpenReadStream());
            uploaded.Add(new UploadedFile(Path.GetFileName(file.FileName), await reader.ReadToEndAsync(cancellationToken)));
        }

        try
        {
            var results = await knowledgeService.UploadAsync(departmentId, flush, uploaded, cancellationToken);
            return results.Concat(rejected).ToList();
        }
        catch (UploadDepartmentNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
