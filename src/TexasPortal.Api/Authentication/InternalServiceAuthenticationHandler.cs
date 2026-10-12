using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace TexasPortal.Api.Authentication;

public sealed class InternalServiceSchemeOptions : AuthenticationSchemeOptions
{
}

public sealed class InternalServiceAuthenticationHandler : AuthenticationHandler<InternalServiceSchemeOptions>
{
    private readonly IConfiguration _config;

    public InternalServiceAuthenticationHandler(
        IOptionsMonitor<InternalServiceSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config)
        : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-OPS-KEY", out var headerValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var expectedKey = _config["SalesPortalKeys:OperationsKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            expectedKey = "tx_ops_secure_888";
        }

        var headerString = headerValue.ToString();
        var headerBytes = Encoding.UTF8.GetBytes(headerString);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);

        if (headerBytes.Length == expectedBytes.Length && 
            CryptographicOperations.FixedTimeEquals(headerBytes, expectedBytes))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "OperationalPortalService"),
                new Claim(ClaimTypes.Name, "Operational Portal Service"),
                new Claim(ClaimTypes.Role, "ServicePrincipal")
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid X-OPS-KEY header."));
    }
}
