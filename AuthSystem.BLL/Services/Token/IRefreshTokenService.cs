

namespace AuthSystem.BLL.Services;

public interface IRefreshTokenService
{
    RefreshTokenDto GenerateRefreshToken();
    int SaveTokenInCookie(string token, DateTime expiration);
    Task<CommonResponse> RefreshToken();
    Task<CommonResponse> RevokeRefreshTokenAsync(string token);
}
