using System.Security.Claims;
using System.Text;
using DG_viajes.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DG_viajes.Services;

public static class JwtConfig
{
    public const string Issuer = "DGViajes";
    public const string Audience = "DGViajesApi";
    public const int MinutosVigencia = 60;
}

public class GeneradorJwt
{
    private readonly string clave;

    public GeneradorJwt(IConfiguration config)
    {
        clave = config["Jwt:Key"]
                ?? throw new InvalidOperationException("Falta Jwt:Key. Cargala con: dotnet user-secrets set \"Jwt:Key\" \"un-texto-largo-y-secreto\"");
    }

    public (string Token, DateTime Expira) Generar(Usuario u)
    {
        var expira = DateTime.UtcNow.AddMinutes(JwtConfig.MinutosVigencia);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = JwtConfig.Issuer,
            Audience = JwtConfig.Audience,
            Expires = expira,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
                new Claim(ClaimTypes.Email, u.Email),
                new Claim(ClaimTypes.Name, $"{u.Nombre} {u.Apellido}"),
                new Claim(ClaimTypes.Role, u.Rol)
            }),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)), SecurityAlgorithms.HmacSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expira);
    }
}