using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingAI.Api.Common;

namespace TicketingAI.Api.Departments;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DepartmentsController(IDepartmentService departmentService) : ControllerBase
{
    // Any logged-in user can read the list (the Users page needs it). Only admins can change it.
    [HttpGet]
    public async Task<List<DepartmentDto>> GetAll() => await departmentService.GetAllAsync();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> Create(DepartmentRequest request)
    {
        var result = await departmentService.CreateAsync(request);
        return result.Department is null ? Fail(result) : Created($"api/departments/{result.Department.Id}", result.Department);
    }

    [HttpPost("load-defaults")]
    [Authorize(Roles = "Admin")]
    public async Task<LoadDefaultsResult> LoadDefaults() => await departmentService.LoadDefaultsAsync();

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, DepartmentRequest request)
    {
        var result = await departmentService.UpdateAsync(id, request);
        return result.Department is null ? Fail(result) : result.Department;
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await departmentService.DeleteAsync(id);
        return result.Error == DepartmentError.None ? NoContent() : Fail(result);
    }

    private ActionResult Fail(DepartmentResult result) => result.Error switch
    {
        DepartmentError.NotFound => NotFound(),
        DepartmentError.NameTaken or DepartmentError.HasUsers => Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
        _ => Problem(result.Message, statusCode: StatusCodes.Status400BadRequest)
    };
}
