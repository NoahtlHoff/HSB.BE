using HSB.BE.Models;
using HSB.BE.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace HSB.BE.Tests
{
    public class TokenServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly TokenService _service;
        private const string ValidKey = "super-secret-key-that-is-long-enough-12345";
        private const string ValidIssuer = "test-issuer";
        private const string ValidAudience = "test-audience";

        public TokenServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns(ValidKey);
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(ValidIssuer);
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns(ValidAudience);

            _service = new TokenService(_mockConfig.Object);
        }

        [Fact]
        public void CreateToken_ReturnsValidToken_WithCorrectClaims()
        {
            // Arrange
            var user = new User { Id = 1, Email = "test@example.com" };

            // Act
            var result = _service.CreateToken(user);

            // Assert
            Assert.NotNull(result.Token);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.Token);

            Assert.Equal(ValidIssuer, token.Issuer);
            Assert.Equal(ValidAudience, token.Audiences.First());
            Assert.Contains(token.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1");
            Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Name && c.Value == "test@example.com");
        }

        [Fact]
        public void CreateToken_IncludesExtraClaims_WhenProvided()
        {
            // Arrange
            var user = new User { Id = 1, Email = "test@example.com" };
            var extraClaims = new List<Claim> { new Claim("role", "admin") };

            // Act
            var result = _service.CreateToken(user, extraClaims);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.Token);

            Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == "admin");
        }

        [Fact]
        public void CreateToken_HasCorrectExpiration()
        {
            // Arrange
            var user = new User { Id = 1, Email = "test@example.com" };

            // Act
            var result = _service.CreateToken(user);

            // Assert
            Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
            Assert.True(result.ExpiresAtUtc <= DateTime.UtcNow.AddHours(1).AddSeconds(5)); // Allow small buffer
        }

        [Fact]
        public void CreateToken_ThrowsException_WhenKeyMissing()
        {
            // Arrange
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns((string?)null);
            var service = new TokenService(_mockConfig.Object);
            var user = new User { Id = 1, Email = "test@example.com" };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => service.CreateToken(user));
        }
    }
}
