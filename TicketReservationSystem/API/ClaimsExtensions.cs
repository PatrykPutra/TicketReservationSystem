using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TicketReservationSystem.API;

public static class ClaimsExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(value, out userId);
    }
}
