using HSB.BE.Dtos;
using HSB.BE.Services;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IAuthService _auth;
	private readonly IEmailService _emailService;
	public AuthController(IEmailService emailService, IAuthService auth)
	{
		_emailService = emailService;
		_auth = auth;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] UserInputDto dto, CancellationToken ct)
	{
		try
		{
			var result = await _auth.RegisterAsync(dto, ct);
			_ = _emailService.SendWelcomeEmailAsync(dto.Email, dto.Name);

			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] UserInputDto dto, CancellationToken ct)
	{
		try
		{
			var result = await _auth.LoginAsync(dto, ct);
			return Ok(result);
		}
		catch (UnauthorizedAccessException)
		{
			return Unauthorized(new { message = "Invalid email or password." });
		}
	}
}