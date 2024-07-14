

namespace AuthSystem.DAL.Repository;

public class RefreshTokenRepo : IRefreshTokenRepo
{
    private readonly AppDbContext _context;
    public RefreshTokenRepo(AppDbContext context)
    {
        _context = context;
    }
    public async Task<bool> CreateAsync(RefreshToken token)
    {
        try
        {
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsUserHaveActiveRefreshTokenAsync(string userId)
    {
        return _context.Set<RefreshToken>()
            .Any(t => t.UserId == userId && t.RevokedOn == null && DateTime.UtcNow <= t.ExpiresOn);
    }

    public async Task<RefreshToken> GetActiveTokenForUserAsync(string userId)
    {
        return await _context.Set<RefreshToken>()
            .SingleOrDefaultAsync(t => t.UserId == userId && t.RevokedOn == null && DateTime.UtcNow <= t.ExpiresOn) ?? null!;
    }

    public async Task<RefreshToken> GetTokenAsync(string token)
    {
        return await _context.Set<RefreshToken>().FirstOrDefaultAsync(t => t.Token == token) ?? null!;
    }

    public bool Update(RefreshToken token)
    {
        try
        {
            _context.Set<RefreshToken>().Update(token);
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
