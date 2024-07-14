
namespace AuthSystem.BLL.Services;

public interface IRoleService
{
	Task<CommonResponse> CreateRole(AddRoleDto model);
	Task<CommonResponse> DeleteRole(string roleId);
}
