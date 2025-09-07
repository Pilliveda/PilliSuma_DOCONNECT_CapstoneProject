// DoConnect.Tests/Unit/Services/JwtTokenService_MoqTests.cs
using System.IdentityModel.Tokens.Jwt;
using DoConnect.Api.Models;
using DoConnect.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DoConnect.Tests.Unit.Services
{
    public class JwtTokenService_MoqTests
    {
        [Fact]
        public void Create_returns_token_and_expiry_and_contains_basic_claims()
        {
            // Arrange
            var settings = new JwtSettings
            {
                Key = "super_secret_key_for_tests_1234567890",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpiresMinutes = 30
            };

            var opt = new Mock<IOptions<JwtSettings>>();
            opt.SetupGet(o => o.Value).Returns(settings);

            var svc = new JwtTokenService(opt.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "alice",
                Email = "alice@example.com",
                Role = RoleType.User
            };

            // Act
            var (token, expires) = svc.Create(user);

            // Assert
            token.Should().NotBeNullOrWhiteSpace();
            expires.Should().BeAfter(DateTime.UtcNow).And.BeBefore(DateTime.UtcNow.AddHours(1));

            // (Optional) Inspect token payload (no signature validation needed here)
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        }
    }
}
