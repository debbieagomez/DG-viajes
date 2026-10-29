using DG_viajes.Models;
using DG_viajes.Repositories;
using DG_viajes.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

[ApiController]
[Route("api")]
public class AuthApiController : ControllerBase
{
    private readonly RepositorioUsuarioApi repo;
    private readonly GeneradorJwt jwt;
    private readonly IWebHostEnvironment env;
    private readonly IPasswordHasher<Usuario> hasher; // el mismo que registró Deborah en Program.cs

    public AuthApiController(IConfiguration config, IWebHostEnvironment env, IPasswordHasher<Usuario> hasher)
    {
        repo = new RepositorioUsuarioApi(config);
        jwt = new GeneradorJwt(config);
        this.env = env;
        this.hasher = hasher;
    }

    // POST /api/auth/login  { "email": "...", "password": "..." }
    [HttpPost("auth/login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrEmpty(req.Password))
            return BadRequest(new { error = "El email y la contraseña son obligatorios." });

        var u = repo.ObtenerPorEmail(req.Email.Trim());
        // Mismo mensaje para usuario inexistente, inactivo o contraseña incorrecta.
        if (u == null || !u.Activo || !PasswordValida(u, req.Password))
            return Unauthorized(new { error = "Credenciales inválidas." });

        var (token, expira) = jwt.Generar(u);
        return Ok(new
        {
            token,
            tipo = "Bearer",
            expiraEn = expira,
            usuario = new { u.Id, u.Email, u.Nombre, u.Apellido, u.Rol }
        });
    }

    // Solo en Development. Pone la contraseña "Test1234" a los usuarios de prueba que todavía tienen el hash pendiente.
    // TODO: borrar este endpoint antes de la entrega final.
    [HttpPost("dev/inicializar-passwords")]
    public IActionResult InicializarPasswords()
    {
        if (!env.IsDevelopment()) return NotFound();
        int cantidad = repo.InicializarPasswords(() => hasher.HashPassword(new Usuario(), "Test1234"));
        return Ok(new { actualizados = cantidad, password = "Test1234" });
    }

    private bool PasswordValida(Usuario u, string password)
    {
        try
        {
            return hasher.VerifyHashedPassword(u, u.PasswordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false; // hash inválido, por ejemplo el PENDIENTE_GENERAR_HASH de los datos de prueba
        }
    }
}