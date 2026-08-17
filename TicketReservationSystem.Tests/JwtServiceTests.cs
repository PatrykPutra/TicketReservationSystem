using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using TicketReservationSystem.Application.Authentication;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ForValidInputs_ReturnsDecodableTokenWithExpectedClaims()
    {
        var settings = new JwtSettings
        {
            Key = "super-secret-test-key-that-is-long-enough-for-hs256-signing!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 30,
        };
        var service = new JwtService(Options.Create(settings));
        var userId = UserId.CreateUnique();

        var token = service.GenerateToken(userId, "user@test.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(userId.Value.ToString(), jwt.Payload[JwtRegisteredClaimNames.Sub].ToString());
        Assert.Equal("user@test.com", jwt.Payload[JwtRegisteredClaimNames.Email].ToString());
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Equal("TestAudience", Assert.Single(jwt.Audiences));
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(29));
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(30).AddSeconds(5));
    }
}