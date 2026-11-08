using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CoachDesk.Api.Services;

public sealed class SupabaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    SupabaseClient supabase) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();

        var token = header[7..].Trim();
        if (string.IsNullOrWhiteSpace(token)) return AuthenticateResult.Fail("Token Bearer vacío.");

        try
        {
            var user = await supabase.GetUserAsync(token, Context.RequestAborted);
            if (user is null) return AuthenticateResult.Fail("El token de Supabase no es válido o expiró.");

            var identity = new ClaimsIdentity(Scheme.Name);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
        }
        catch (SupabaseRequestException exception)
        {
            Logger.LogWarning(exception, "Supabase rechazó la validación del token.");
            return AuthenticateResult.Fail("No fue posible validar el token de Supabase.");
        }
    }
}
