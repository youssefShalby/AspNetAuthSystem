
namespace AuthSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
	private readonly IRoleService _roleService;
	public RolesController(IRoleService roleService)
	{
		_roleService = roleService;
	}

	[HttpPost("AddRole")]
	public async Task<ActionResult> AddRole(AddRoleDto role)
	{
		if(!ModelState.IsValid) 
		{
			return BadRequest(ModelState);
		}

		var result = await _roleService.CreateRole(role);
		if(!result.IsSuccessed)
		{
			return BadRequest(result);
		}
		return Ok(result);
	}

	[HttpDelete("{name:alpha}")]
	public async Task<ActionResult> RemoveRole(string name)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var result = await _roleService.DeleteRole(name);
		if (!result.IsSuccessed)
		{
			return BadRequest(result);
		}
		return Ok(result);
	}

}
