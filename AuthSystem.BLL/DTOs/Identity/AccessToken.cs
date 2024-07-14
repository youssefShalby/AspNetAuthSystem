namespace AuthSystem.BLL.DTOs;

public class AccessToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpirationTime { get; set; }
    public AccessToken(string token, DateTime expireTime)
    {
        Token = token;
        ExpirationTime = expireTime;
    }
}
