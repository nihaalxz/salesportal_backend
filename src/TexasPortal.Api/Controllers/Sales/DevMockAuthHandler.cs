using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace TexasPortal.Api.Controllers.Sales;

public class DevMockAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevMockAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = authHeaderValues.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader["Bearer ".Length..].Trim();

        Claim[] claims;
        string schemeName = "DevMockScheme";

        if (token == "dev-token-salesman-a")
        {
            claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "John Salesman"),
                new Claim(ClaimTypes.Email, "john.sales@texastravelsbh.com"),
                new Claim(ClaimTypes.Role, "Salesman")
            };
        }
        else if (token == "dev-token-salesman-b")
        {
            claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Name, "Sarah Salesman"),
                new Claim(ClaimTypes.Email, "sarah.sales@texastravelsbh.com"),
                new Claim(ClaimTypes.Role, "Salesman")
            };
        }
        else if (token == "dev-token-no-permission")
        {
            claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "3"),
                new Claim(ClaimTypes.Name, "Guest User"),
                new Claim(ClaimTypes.Email, "guest@texastravelsbh.com"),
                new Claim(ClaimTypes.Role, "Guest")
            };
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid mock token"));
        }

        var identity = new ClaimsIdentity(claims, schemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, schemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
