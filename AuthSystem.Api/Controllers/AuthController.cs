﻿

namespace E_Commerce.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
	private readonly IUserService _userService;
    public AuthController(IUserService userService)
    {
		_userService = userService;
    }

	[HttpPost("Register")]
	public async Task<ActionResult> Register(RegisterUserDto model)
	{
		if (model is null || !ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var result = await _userService.RegisterAsync(model);

		if (!result.IsSuccessed)
		{
			return BadRequest(result);
		}

		string userId = (string)result.AdditionalInfo;

		//> send the UserId in Body of Response
		return CreatedAtAction(nameof(ConfirmEmail), new { UserId = userId });
	}

	[HttpPost("ConfirmEmail")]
	public async Task<ActionResult> ConfirmEmail(VerificationCodeDto model)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		var result = await _userService.ConfirmEmailAsync(model);
		if (result.IsSuccessed)
		{
			return Ok(result);
		}
		return BadRequest(result);
	}

	[HttpPost("Login")]
	public async Task<ActionResult> Login(LoginDto model)
	{
		if(!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var result = await _userService.LoginAsync(model);
		if(!result.IsSuccessed)
		{
			return BadRequest(result);
		}

		//> AdditionalInfo contain the token and expire time
		return Ok(result.AdditionalInfo);
	}

	[HttpPost("ForgetPassword")]
	public async Task<ActionResult> ForgetPassword([FromHeader]string email)
	{
		if(string.IsNullOrEmpty(email))
		{
			return BadRequest("email is not valid");
		} 

		//> execute the action process
		var result = await _userService.ForgetPasswordAsync(email);
		if(!result.IsSuccessed)
		{
			return BadRequest(result); //> 400
		}
		return Ok(result); //> 200
	}

	[HttpPost("ResetPassword")]
	public async Task<ActionResult> ResetPassword([FromBody]ResetPasswordDto model, [FromHeader]string token)
	{
		if(!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}
		var result = await _userService.ResetPasswordAsync(model, token);
		if (!result.IsSuccessed)
		{
			return BadRequest(result); //> 400
		}
		return Ok(result); //> 200
	}

	[HttpPost("Logout")]
	public async Task<ActionResult> Logout()
	{
		return Ok(await _userService.LogoutAsync());
	}

	[HttpPost("RomveAcc")]
	public async Task<ActionResult> ReomveAccount(RemoveAccountDto model)
	{
		if(!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}
		var result = await _userService.RemoveAccountAsync(model);
		if (!result.IsSuccessed)
		{
			return BadRequest(result); //> 400
		}
		return Ok(result); //> 200
	}

	[HttpPost("ResendConfirmationEmail/{email}")]
	public async Task<ActionResult> ResendConfirmationEmail(string email)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var result = await _userService.ResendConfirmationEmail(email);
		if (!result.IsSuccessed)
		{
			return BadRequest(result); //> 400
		}
		return Ok(result); //> 200
	}
}
