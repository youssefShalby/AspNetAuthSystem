	
namespace E_Commerce.BLL.Services;

public interface IHandlerService
{
	Task<CommonResponse> RegisterHandlerAsync(RegisterUserDto model, string mainRole, params string[] otherRoles);
}
