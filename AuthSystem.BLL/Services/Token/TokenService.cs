

using AuthSystem.BLL.Settings;

namespace AuthSystem.BLL.Services;

public class TokenService : ITokenService, IRefreshTokenService
{
	private readonly IConfiguration _configuration;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly JWT _jwt;
	private readonly IRefreshTokenRepo _refreshTokenRepo;
	public TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor,
		JWT jwt, IRefreshTokenRepo refreshTokenRepo)
	{
		_configuration = configuration;
		_userManager = userManager;
		_httpContextAccessor = httpContextAccessor;
		_jwt = jwt;
		_refreshTokenRepo = refreshTokenRepo;
	}
	public JwtSecurityToken CreateToken(List<Claim> claims, DateTime expireationTime)
	{
		return new JwtSecurityToken(
			issuer: _configuration["JWT:Issuer"],
			audience: _configuration["JWT:Audiences"],
			notBefore: DateTime.Now,
			claims: claims,
			expires: expireationTime,
			signingCredentials: GetCredentials()
			);
	}

	public SigningCredentials GetCredentials()
	{
		return new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256Signature);
	}
	public SymmetricSecurityKey GetKey()
	{
		var keyInBytes = Encoding.ASCII.GetBytes(GetKeyAsString());
		return new SymmetricSecurityKey(keyInBytes);
	}

	public string GetKeyAsString()
	{
		return _configuration.GetValue<string>("Jwt:TokenKey");
	}

	public JwtSecurityToken ReadToken(string token)
	{
		//> will return token as json
		JwtSecurityTokenHandler handler = new();

		//> read and cast the token
		var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
		if (jsonToken is null) return null!;
		return jsonToken;
	}

	public string ExtractClaimFromToken(string token, string tokenClaim)
	{
		//> read token
		var jsonToken = ReadToken(token);
		var claimValue = jsonToken.Claims.FirstOrDefault(claim => claim.Type == tokenClaim)?.Value;
		return claimValue ?? null!;
	}

	public DateTime GetExpirationTimeOfToken(string token)
	{
		//> read token and access ValidTo prop
		return ReadToken(token).ValidTo;
	}
	public bool IsTokenExpired(string token)
	{
		DateTime expireTime = ReadToken(token).ValidTo;
		return DateTimeOffset.UtcNow >= expireTime;
	}

	public async Task<string> CreateAccessTokenAsync(ApplicationUser user)
	{
		//> get user claims from database
		var userClaims = await _userManager.GetClaimsAsync(user);

		if (userClaims is null) return "NA";

		//> add JWTRegisteredClaimNames.Jti to the claims => Id of token by using Guid().NewGuid().ToString()
		userClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

		//> generate the token by claims
		var generateToken = CreateToken(userClaims.ToList(), DateTime.Now.AddMinutes(int.Parse(_jwt.TokenExpirePerMin ?? "5")));
		return new JwtSecurityTokenHandler().WriteToken(generateToken);
	}

	public int SaveTokenInCookie(string token, DateTime expiration)
	{
		//> get HttpContext from the Service
		var httpContext = _httpContextAccessor.HttpContext;
		if(httpContext is null) return -1;
		

		//> create secure cookie
		var cookieOption = new CookieOptions
		{
			Expires = expiration,
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.None,
			IsEssential = true,
		};

		httpContext.Response.Cookies.Append($"refreshToken", token, cookieOption);
		return 0;
	}

    public RefreshTokenDto GenerateRefreshToken()
    {
		byte[] randomNumbers = new byte[32];
		using var generator = new RNGCryptoServiceProvider();

		generator.GetBytes(randomNumbers);

		return new RefreshTokenDto
		{
			CreatedOn = DateTime.Now,
			ExpiresOn = DateTime.Now.AddDays(int.Parse(_jwt.TokenExpirePerDay)),
			Token = Convert.ToBase64String(randomNumbers)
		};
    }

    public async Task<CommonResponse> RefreshToken()
    {
		var httpContext = _httpContextAccessor.HttpContext;
		var refreshToken = httpContext?.Request.Cookies["refreshToken"];

		if(refreshToken is null)
		{
			return new CommonResponse("there is no token to refresh..!!", false);
		}

		var refreshTokenModel = await _refreshTokenRepo.GetTokenAsync(refreshToken);
		if (!refreshTokenModel.IsActive)
		{
			return new CommonResponse("Invalid token..!!", false);
		}

		refreshTokenModel.RevokedOn = DateTime.UtcNow;
		var newToken = GenerateRefreshToken();
		var newRefreshToken = new RefreshToken
		{
			RevokedOn = null,
			ExpiresOn = newToken.ExpiresOn,
			Id = Guid.NewGuid(),
			CreatedOn = newToken.CreatedOn,
			Token = newToken.Token,
			UserId = refreshTokenModel.UserId
		};

		await _refreshTokenRepo.CreateAsync(newRefreshToken);

		var saved = SaveTokenInCookie(newRefreshToken.Token, newRefreshToken.ExpiresOn);
		if(saved == 0)
		{
			var user =  await _userManager.FindByIdAsync(refreshTokenModel.UserId);
			var accessToken = await CreateAccessTokenAsync(user);

			var token = new AccessToken(accessToken, GetExpirationTimeOfToken(accessToken));
			return new CommonResponse("token refreshed", true, null!, token);
		}
		return new CommonResponse("token refreshed but cannot saved it in cookie..!", true);
    }

    public async Task<CommonResponse> RevokeRefreshTokenAsync(string token)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var tokenFromCookie = httpContext?.Request.Cookies["refreshToken"];

		var refreshToken = token ?? tokenFromCookie;

        if (refreshToken is null)
        {
            return new CommonResponse("there is no token to refresh..!!", false);
        }

        var refreshTokenModel = await _refreshTokenRepo.GetTokenAsync(refreshToken);

		if (refreshTokenModel is null)
		{
			return new CommonResponse("Token Invalid..!!", false);
		}

        if (!refreshTokenModel.IsActive)
        {
            return new CommonResponse("Invalid token..!!", false);
        }

		refreshTokenModel.RevokedOn = DateTime.Now;
		_refreshTokenRepo.Update(refreshTokenModel);

		httpContext?.Response.Cookies.Delete("refreshToken");

		return new CommonResponse("refresh token revoked..!!", true);
    }
}
