using HSB.BE.Dtos;
using HSB.BE.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HSB.BE.Services
{
	public interface ITokenService
	{
		TokenResult CreateToken(User user, IEnumerable<Claim>? extraClaims = null);
	}

	public class TokenService : ITokenService
	{
		private readonly IConfiguration _config;
		public TokenService(IConfiguration config) => _config = config;

		public TokenResult CreateToken(User user, IEnumerable<Claim>? extraClaims = null)
		{
			var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
			var issuer = _config["Jwt:Issuer"];
			var audience = _config["Jwt:Audience"];

			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			var claims = new List<Claim>
			{
				new(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new(ClaimTypes.Name, user.Email)
			};
			if (extraClaims is not null) claims.AddRange(extraClaims);

			var expires = DateTime.UtcNow.AddHours(1);

			var token = new JwtSecurityToken(
				issuer: issuer,
				audience: audience,
				claims: claims,
				notBefore: DateTime.UtcNow,
				expires: expires,
				signingCredentials: credentials);

			return new TokenResult
			{
				Token = new JwtSecurityTokenHandler().WriteToken(token),
				ExpiresAtUtc = expires
			};
		}

	}
}
