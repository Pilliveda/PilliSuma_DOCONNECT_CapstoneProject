using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using DoConnect.Api.Models;
using DoConnect.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DoConnect.Tests.Unit
{
    public class JwtTokenServiceTests
    {
        private static JwtTokenService Create(out JwtSettings settings)
        {
            settings = new JwtSettings
            {
                Key = "THIS_IS_A_DEMO_SECRET_KEY_FOR_TESTS_1234567890", // >= 32 chars
                Issuer = "DoConnect.Tests",
                Audience = "DoConnect.Tests",
                ExpiresMinutes = 30
            };
            return new JwtTokenService(Options.Create(settings));
        }

        [Fact]
        public void Create_Should_Emit_Expected_Claims()
        {
            var svc = Create(out var cfg);
            var user = new User
            {
                Id = System.Guid.NewGuid(),
                Username = "alice",
                Email = "alice@example.com",
                Role = RoleType.Admin
            };

            var (token, expires) = svc.Create(user);

            token.Should().NotBeNullOrWhiteSpace();
            expires.Should().BeAfter(System.DateTime.UtcNow);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Your service sets: sub, unique_name, email, ClaimTypes.Role, ClaimTypes.NameIdentifier
            jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value
               .Should().Be(user.Id.ToString());

            jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value
               .Should().Be(user.Username);

            jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value
               .Should().Be(user.Email);

            jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value
               .Should().Be(user.Role.ToString());

            jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value
               .Should().Be(user.Id.ToString());
        }
    }
}
