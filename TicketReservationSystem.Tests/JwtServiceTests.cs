using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using TicketReservationSystem.Application.Authentication;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class JwtServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Key = "super-secret-test-key-that-is-long-enough-for-hs256-signing!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpiryMinutes = 30,
    };

    [Fact]
    public void GenerateToken_ForValidInputs_ReturnsDecodableToken()
    {
        var service = new JwtService(Options.Create(Settings));
        var userId = UserId.CreateUnique();

        var token = service.GenerateToken(userId, "user@test.com");

        Assert.IsType<JwtSecurityToken>(new JwtSecurityTokenHandler().ReadJwtToken(token));
    }

    [Fact]
    public void GenerateToken_ForValidInputs_SetsSubjectClaim()
    {
        var service = new JwtService(Options.Create(Settings));
        var userId = UserId.CreateUnique();

        var token = service.GenerateToken(userId, "user@test.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(userId.Value.ToString(), jwt.Payload[JwtRegisteredClaimNames.Sub].ToString());
    }

    [Fact]
    public void GenerateToken_ForValidInputs_SetsEmailClaim()
    {
        var service = new JwtService(Options.Create(Settings));
        var userId = UserId.CreateUnique();

        var token = service.GenerateToken(userId, "user@test.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("user@test.com", jwt.Payload[JwtRegisteredClaimNames.Email].ToString());
    }

    [Fact]
    public void GenerateToken_ForValidInputs_SetsIssuerAndAudience()
    {
        var service = new JwtService(Options.Create(Settings));
        var userId = UserId.CreateUnique();

        var token = service.GenerateToken(userId, "user@test.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Equal("TestAudience", Assert.Single(jwt.Audiences));
    }

    [Fact]
    public void GenerateToken_ForValidInputs_SetsExpiry()
    {
        var service = new JwtService(Options.Create(Settings));
        var userId = UserId.CreateUnique();

        var token = service.GenerateToken(userId, "user@test.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(29));
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(30).AddSeconds(5));
    }
}