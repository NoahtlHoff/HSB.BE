using HSB.BE.Dtos;
using HSB.BE.Models;
using HSB.BE.Repository;
using Microsoft.AspNetCore.Identity;

namespace HSB.BE.Services
{
	public interface IAuthService
	{
		Task<AuthResponseDto> RegisterAsync(UserInputDto dto, CancellationToken ct = default);

		Task<AuthResponseDto> LoginAsync(UserInputDto dto, CancellationToken ct = default);
	}
	public class AuthService : IAuthService
	{
		private readonly IUserRepository _users;
		private readonly IPasswordHasher<User> _hasher;
		private readonly ITokenService _tokens;

		public AuthService(IUserRepository users, IPasswordHasher<User> hasher, ITokenService tokens)
		{
			_users = users;
			_hasher = hasher;
			_tokens = tokens;
		}

		public async Task<AuthResponseDto> RegisterAsync(UserInputDto dto, CancellationToken ct = default)
		{
			var email = dto.Email.Trim().ToLowerInvariant();

			if (await _users.EmailExistsAsync(email, ct))
				throw new InvalidOperationException("Email is already registered.");

			var user = new User { Email = email };
			user.PasswordHash = _hasher.HashPassword(user, dto.Password);

			user = await _users.AddAsync(user, ct);

			var token = _tokens.CreateToken(user);
			return new AuthResponseDto
			{
				Token = token.Token,
				ExpiresAtUtc = token.ExpiresAtUtc,
				Email = user.Email,
				UserId = user.Id
			};
		}

		public async Task<AuthResponseDto> LoginAsync(UserInputDto dto, CancellationToken ct = default)
		{
			var email = dto.Email.Trim().ToLowerInvariant();
			var user = await _users.GetByEmailAsync(email, ct);
			if (user is null)
				throw new UnauthorizedAccessException("Invalid credentials.");

			var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
			if (result == PasswordVerificationResult.Failed)
				throw new UnauthorizedAccessException("Invalid credentials.");

			var token = _tokens.CreateToken(user);
			return new AuthResponseDto
			{
				Token = token.Token,
				ExpiresAtUtc = token.ExpiresAtUtc,
				Email = user.Email,
				UserId = user.Id
			};
		}
	}
}
