using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Domain.Entities;
using AuctionNest.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuctionNest.Infrastructure.Services;

public sealed class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(File.ReadAllText(_settings.PrivateKeyPath));
        _privateKey = new RsaSecurityKey(privateRsa);

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(File.ReadAllText(_settings.PublicKeyPath));
        _publicKey = new RsaSecurityKey(publicRsa);
    }

    public string GenerateAccessToken(User user)
    {
        var credentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public Guid? ValidateAccessToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                IssuerSigningKey = _publicKey,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }
}