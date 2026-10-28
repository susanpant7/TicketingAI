using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketingAI.Api.Departments;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DepartmentsController(IDepartmentRepository departments) : ControllerBase
{
    [HttpGet]
    public async Task<List<DepartmentDto>> GetAll() =>
        (await departments.GetAllAsync()).Select(d => new DepartmentDto(d.Id, d.Name)).ToList();
}
