

namespace AuthSystem.DAL.Repository;

public interface IRefreshTokenRepo
{
    Task<bool> CreateAsync(RefreshToken token);
    bool IsUserHaveActiveRefreshTokenAsync(string userId);
    Task<RefreshToken> GetActiveTokenForUserAsync(string userId);
    Task<RefreshToken> GetTokenAsync(string token);
    bool Update(RefreshToken token);
}
