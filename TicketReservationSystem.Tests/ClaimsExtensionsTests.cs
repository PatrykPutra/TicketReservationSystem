using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TicketReservationSystem.API;

namespace TicketReservationSystem.Tests;

public class ClaimsExtensionsTests
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void TryGetUserId_WhenNameIdentifierClaimPresent_ReturnsTrueWithUserId()
    {
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        var result = principal.TryGetUserId(out var parsedUserId);

        Assert.True(result);
        Assert.Equal(userId, parsedUserId);
    }

    [Fact]
    public void TryGetUserId_WhenOnlySubClaimPresent_ReturnsTrueWithUserId()
    {
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));

        var result = principal.TryGetUserId(out var parsedUserId);

        Assert.True(result);
        Assert.Equal(userId, parsedUserId);
    }

    [Fact]
    public void TryGetUserId_WhenClaimValueIsNotGuid_ReturnsFalse()
    {
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        var result = principal.TryGetUserId(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetUserId_WhenNoClaimPresent_ReturnsFalse()
    {
        var principal = CreatePrincipal();

        var result = principal.TryGetUserId(out _);

        Assert.False(result);
    }
}
