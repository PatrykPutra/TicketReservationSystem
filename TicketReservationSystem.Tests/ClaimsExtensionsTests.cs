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
        // Arrange
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        // Act
        var result = principal.TryGetUserId(out var parsedUserId);

        // Assert
        Assert.True(result);
        Assert.Equal(userId, parsedUserId);
    }

    [Fact]
    public void TryGetUserId_WhenOnlySubClaimPresent_ReturnsTrueWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));

        // Act
        var result = principal.TryGetUserId(out var parsedUserId);

        // Assert
        Assert.True(result);
        Assert.Equal(userId, parsedUserId);
    }

    [Fact]
    public void TryGetUserId_WhenClaimValueIsNotGuid_ReturnsFalse()
    {
        // Arrange
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        // Act
        var result = principal.TryGetUserId(out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryGetUserId_WhenNoClaimPresent_ReturnsFalse()
    {
        // Arrange
        var principal = CreatePrincipal();

        // Act
        var result = principal.TryGetUserId(out _);

        // Assert
        Assert.False(result);
    }
}
