using HSB.BE.Dtos;
using HSB.BE.Models;
using HSB.BE.Repository;
using HSB.BE.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSB.BE.Tests
{
	public class AuthServiceTests
	{
		[Fact]
		public async Task RegisterAsync_ReturnsAuthResponse_WhenNewEmail()
		{
			// Arrange
			var dto = new UserInputDto
			{
				Email = " Test@Example.COM ",
				Name = "  Alice  ",
				Password = "P@ssw0rd1"
			};

			var usersMock = new Mock<IUserRepository>();
			var hasherMock = new Mock<IPasswordHasher<User>>();
			var tokensMock = new Mock<ITokenService>();

			usersMock
				.Setup(x => x.EmailExistsAsync("test@example.com", It.IsAny<CancellationToken>()))
				.ReturnsAsync(false);

			hasherMock
				.Setup(h => h.HashPassword(It.IsAny<User>(), dto.Password))
				.Returns("hashed-password");

			User? addedUser = null;
			usersMock
				.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((User u, CancellationToken ct) =>
				{
					u.Id = 1;
					addedUser = u;
					return u;
				});

			var tokenResult = new TokenResult { Token = "jwt-token", ExpiresAtUtc = DateTime.UtcNow.AddHours(1) };
			tokensMock
				.Setup(t => t.CreateToken(It.IsAny<User>(), It.IsAny<IEnumerable<Claim>>()))
				.Returns(tokenResult);

			var svc = new AuthService(usersMock.Object, hasherMock.Object, tokensMock.Object);

			// Act
			var result = await svc.RegisterAsync(dto, CancellationToken.None);

			// Assert
			Assert.NotNull(result);
			Assert.Equal("test@example.com", result.Email);
			Assert.Equal(1, result.UserId);
			Assert.Equal("jwt-token", result.Token);

			Assert.NotNull(addedUser);
			Assert.Equal("test@example.com", addedUser!.Email);
			Assert.Equal("Alice", addedUser.Name);
			Assert.Equal("hashed-password", addedUser.PasswordHash);
		}
	}
}
