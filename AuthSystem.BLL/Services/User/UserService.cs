﻿
namespace AuthSystem.BLL.Services;

public class UserService : IUserService
{
	private readonly RoleManager<IdentityRole> _roleManager;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IHandlerService _handlerService;
	private readonly ITokenService _tokenService;
	private readonly IRefreshTokenService _refreshTokenService;
	private readonly SignInManager<ApplicationUser> _signInManager;
	private readonly IConfiguration _configuration;
	private readonly IEmailService _emailService;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly IRefreshTokenRepo _refreshTokenRepo;
    public UserService(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IHandlerService handlerService, ITokenService tokenService, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, IEmailService emailService, IHttpContextAccessor httpContextAccessor, IRefreshTokenService refreshTokenService, IRefreshTokenRepo refreshTokenRepo)
    {
		_roleManager = roleManager;
		_userManager = userManager;
		_handlerService = handlerService;
		_tokenService = tokenService;
		_signInManager = signInManager;
		_configuration = configuration;
		_emailService = emailService;
		_httpContextAccessor = httpContextAccessor;
		_refreshTokenService = refreshTokenService;
		_refreshTokenRepo = refreshTokenRepo;
    }

	public async Task<CommonResponse> RegisterAsync(RegisterUserDto model)
	{
		var UserRole = await _roleManager.FindByNameAsync("User");
		if(UserRole is null)
		{
			return new CommonResponse("cannot create user because the 'User' role is not found, create the 'User' role first and then create user", false);
		}
		return await _handlerService.RegisterHandlerAsync(model, UserRole.Name, UserRole.Name);
	}

	public async Task<CommonResponse> ConfirmEmailAsync(VerificationCodeDto model)
	{
		var user = await _userManager.FindByIdAsync(model.UserId);
		if(user is null)
		{
			return new CommonResponse("user not registered...", false);
		}

		if(user.EmailConfirmed)
		{
			return new CommonResponse("Email already confirmed..!!", true);
		}

		//> get token and verification code from db
		var token = user.ActivationToken;
		var verificationCode = user.ConfirmEmailCode;

		if (token is null || verificationCode is null) 
		{
			return new CommonResponse("verification code expired, order new one", false);
		}

		if(_tokenService.IsTokenExpired(token))
		{
			return new CommonResponse("the entred code is expired, order new one", false);
		}

		//> ensure the code is right
		if(model.Code == null || verificationCode != model.Code || model.Code.Length < 4)
		{
			return new CommonResponse("the code is not valid", false);
		}

		//> if the code is right and not expired, so confirm email
		user.EmailConfirmed = true;
		IdentityResult result = await _userManager.UpdateAsync(user);
		if(!result.Succeeded)
		{
			var errors = Helper.GetErrorsOfIdentityResult(result.Errors);
			return new CommonResponse("cannot confirm email right now, try again later", false, errors);
		}

		//> if confirmed success, remove token and confirmationCode from db to prevent the storage overhead
		user.ActivationToken = null!;
		user.ConfirmEmailCode = null!;
		await _userManager.UpdateAsync(user);



		//> then, create login token and save it in the cookie after email confirmation process to make user loged in
		string accessToken = await _tokenService.CreateAccessTokenAsync(user);
		if(accessToken == "NA")
		{
			return new CommonResponse("cannot create access token", true);
		}

		var refreshToken = _refreshTokenService.GenerateRefreshToken();
		var newRefToken = new RefreshToken
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			CreatedOn = refreshToken.CreatedOn,
			RevokedOn = refreshToken.RevokedOn,
			Token  = refreshToken.Token,
			ExpiresOn = refreshToken.ExpiresOn,
		};

		bool created = await _refreshTokenRepo.CreateAsync(newRefToken);
		if (!created)
		{
			return new CommonResponse("cannot create refresh token", true);
		}

		//> save login token in the cookie
		int added = _refreshTokenService.SaveTokenInCookie(refreshToken.Token, refreshToken.ExpiresOn);
		if(added == -1)
		{
			return new CommonResponse("cannot save refresh token in cookie", true);
		}

		AccessToken JWTAccessToken = new AccessToken(accessToken, _tokenService.GetExpirationTimeOfToken(accessToken));

		return new CommonResponse("User Confirmed Successfully, and logged in now", true, null!, JWTAccessToken);
	}

	public async Task<CommonResponse> LoginAsync(LoginDto model)
	{
		//> check email is true and exist
		ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);
		if(user is null)
		{
			return new CommonResponse("Email Not Found..!", false);
		}

		//> check if there is login token in cookie or not
		var httpContext = _httpContextAccessor.HttpContext;
		var checkLoginToken = httpContext?.Request.Cookies.FirstOrDefault(TK => TK.Key == $"refreshToken");
		if (checkLoginToken?.Value is not null)
		{
			return new CommonResponse("user already loged in", false);
		}

		//> check email is confirmed or not, if not return id to confirm it
		if (!user.EmailConfirmed)
		{
			var verificationModel = new VerificationCodeDto(user.Id);
			return new CommonResponse("email not confirmed, please confirm it then login", false, null!, verificationModel.UserId);
		}

		//> check account is blocked or not
		if(await _userManager.IsLockedOutAsync(user))
		{
			return new CommonResponse("your account is blocked for while, try again later", false);
		}

		//> above checks is right, check the password is right and valid or not
		var IsAhthenticated = await _userManager.CheckPasswordAsync(user, model.Password);

		//> if password is not correct, increment number of tries and tell user pass is not correct
		if(!IsAhthenticated)
		{
			return new CommonResponse("the Password Is Not Correct", false);
		}

		//> if email and password are correct, create the token
		string accessToken = await _tokenService.CreateAccessTokenAsync(user);

		RefreshToken refreshToken = new();
		if (_refreshTokenRepo.IsUserHaveActiveRefreshTokenAsync(user.Id))
		{
			refreshToken = await _refreshTokenRepo.GetActiveTokenForUserAsync(user.Id);
		}
		else
		{
			var refreshTokenDto = _refreshTokenService.GenerateRefreshToken();
			refreshToken = new RefreshToken
			{
				CreatedOn = refreshTokenDto.CreatedOn,
				ExpiresOn = refreshTokenDto.ExpiresOn,
				Id = Guid.NewGuid(),
				UserId = user.Id,
				Token = refreshTokenDto.Token,
				RevokedOn = refreshTokenDto.RevokedOn,
			};

			await _refreshTokenRepo.CreateAsync(refreshToken);

		}

		int added = _refreshTokenService.SaveTokenInCookie(refreshToken.Token, refreshToken.ExpiresOn);
		if(added == -1)
		{
			return new CommonResponse("login success, but cannot save refresh token in cookies", true);
		}

		//> return the token with expiration time
		var tokenResponse = new AccessToken(accessToken, _tokenService.GetExpirationTimeOfToken(accessToken));
		return new CommonResponse("Login Success", true, null!, tokenResponse);
	}

	public async Task<CommonResponse> ForgetPasswordAsync(string email)
	{
		//> get the user by email and check user exist or not
		var user = await _userManager.FindByEmailAsync(email);
		if(user is null)
		{
			return new CommonResponse("User Not Found...!!", false);
		}

		var generateToken = await _userManager.GeneratePasswordResetTokenAsync(user);

		//> clear token from special chars
		var tokenInBytes = Encoding.UTF8.GetBytes(generateToken);
		var token = WebEncoders.Base64UrlEncode(tokenInBytes);

		string url = $"{_configuration.GetValue<string>("AppUrl")}/ResetPassword?email={email}&token={token}";
		string emailBody = _emailService.ResetPasswordEmailBody(url);
		var sended = await _emailService.SendEmailAsync(email, "Reset Password", emailBody, true);
		if(!sended.IsSuccessed)
		{
			return sended;
		}
		return new CommonResponse("Reset Password Requested, check your inbox", true);
	}

	public async Task<CommonResponse> ResetPasswordAsync(ResetPasswordDto model, string token)
	{
		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is null)
		{
			return new CommonResponse("User Not Found...!!", false);
		}

		if(model.NewPassword != model.ConfirmPassword)
		{
			return new CommonResponse("Passswords Does not Match..!!", false);
		}

		//> decode token and extract it
		var decodedToken = WebEncoders.Base64UrlDecode(token);
		var originalToken = Encoding.UTF8.GetString(decodedToken);

		var result = await _userManager.ResetPasswordAsync(user, originalToken, model.NewPassword);
		if(!result.Succeeded)
		{
			return new CommonResponse("cannot change password for now because the link expired, order new one", false);
		}
		return new CommonResponse("password changes success", true);
		
	}

	public async Task<CommonResponse> LogoutAsync()
	{
		//> SignOutAsync => will clear authentication cookies or token
		await _signInManager.SignOutAsync();

		//> delete the loginToken
		var httpContext = _httpContextAccessor.HttpContext;
		httpContext?.Response.Cookies.Delete("refreshToken");

		return new CommonResponse("Signed out success", true);
	}

	public async Task<CommonResponse> RemoveAccountAsync(RemoveAccountDto model)
	{
		if (string.IsNullOrEmpty(model.Email))
		{
			return new CommonResponse("email is not valid..!!", false);
		}

		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is null)
		{
			return new CommonResponse("user not found..!!", false);
		}

		//> check password is valid or not
		var passwordIsValid = await _userManager.CheckPasswordAsync(user, model.Password);
		if(!passwordIsValid)
		{
			return new CommonResponse("Password Is Not Valid..!!", false);
		}

		IdentityResult result = await _userManager.DeleteAsync(user);
		if(!result.Succeeded)
		{
			return new CommonResponse("cannot delete account for now, try again later..!!", false);
		}

		//> if remove success, delete cookies and tokens from browser
		await LogoutAsync();

		return new CommonResponse("Account Deleted.. bye bye", true);
	}

	public async Task<CommonResponse> ResendConfirmationEmail(string email)
	{
		var user = await _userManager.FindByEmailAsync(email);
		if (user is null)
		{
			return new CommonResponse("the email is not exist..!!", false);
		}

		string verificationCode = _emailService.GenerateOTPCode();
		string emailBody = _emailService.GetConfirmationEmailBody(verificationCode, email);

		var userClaims = await _userManager.GetClaimsAsync(user);
		var generateToken = _tokenService.CreateToken(userClaims.ToList(), DateTime.Now.AddMinutes(5));
		string token = new JwtSecurityTokenHandler().WriteToken(generateToken);

		user.ConfirmEmailCode = verificationCode;
		user.ActivationToken = token;

		var updated = await _userManager.UpdateAsync(user);
		if (!updated.Succeeded)
		{
			return new CommonResponse("cannot resend the email confirmation..!!", true);
		}

		var sended = await _emailService.SendEmailAsync(email, "Confirm Email", emailBody, true);
		if (!sended.IsSuccessed)
		{
			return sended;
		}

		return new CommonResponse("the email resended success..!!", true);

	}


}
