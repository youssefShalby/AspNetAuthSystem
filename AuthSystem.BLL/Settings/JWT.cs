namespace AuthSystem.BLL.Settings;

public class JWT
{
    public string TokenKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audiences { get; set; } = string.Empty;
    public string TokenExpirePerMin { get; set; } = string.Empty;
    public string TokenExpirePerDay { get; set; } = string.Empty;
}
