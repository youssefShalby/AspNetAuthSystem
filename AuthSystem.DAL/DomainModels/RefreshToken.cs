

namespace AuthSystem.DAL.DomainModels;
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
    public DateTime CreatedOn { get; set; }
    public DateTime? RevokedOn { get; set; } //> RevokedOn is null (meaning it hasn't been revoked)
    public bool IsActive => RevokedOn == null && !IsExpired;
    public string? UserId { get; set; }
}

