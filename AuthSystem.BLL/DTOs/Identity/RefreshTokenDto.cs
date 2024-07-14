

namespace AuthSystem.BLL.DTOs;

public class RefreshTokenDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? RevokedOn { get; set; } //> RevokedOn is null (meaning it hasn't been revoked)
}
