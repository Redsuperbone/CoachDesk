using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoachDesk.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace CoachDesk.Api.Services;

public sealed class PasswordService
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA512, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2) return false;
        var calculated = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[0]), 210_000, HashAlgorithmName.SHA512, 32);
        return CryptographicOperations.FixedTimeEquals(calculated, Convert.FromBase64String(parts[1]));
    }
}

public sealed class JwtService(IConfiguration configuration)
{
    public (string Token, DateTimeOffset ExpiresAt) Create(User user)
    {
        var expires = DateTimeOffset.UtcNow.AddHours(configuration.GetValue<int?>("Jwt:ExpirationHours") ?? 8);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.")));
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: [new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(ClaimTypes.Role, user.Role.ToString()), new(ClaimTypes.Email, user.Email)],
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
