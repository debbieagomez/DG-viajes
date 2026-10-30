using System.Security.Claims;
using DG_viajes.Models;
using DG_viajes.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

public class CuentaController : Controller
{
    private readonly IRepositorioUsuario repositorioUsuario;
    private readonly IPasswordHasher<Usuario> passwordHasher;
    private readonly IWebHostEnvironment entorno;   //  wwwroot

    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png" };
    private const long TamanioMaximo = 2 * 1024 * 1024;   // 2 MB

    public CuentaController(
        IRepositorioUsuario repositorioUsuario,
        IPasswordHasher<Usuario> passwordHasher,
        IWebHostEnvironment entorno)
    {
        this.repositorioUsuario = repositorioUsuario;
        this.passwordHasher = passwordHasher;
        this.entorno = entorno;
    }

    // ---------- LOGIN ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Ingresá el email y la contraseña.");
            return View();
        }

        var usuario = repositorioUsuario.ObtenerPorEmail(email.Trim());

        if (usuario == null
            || passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password)
               == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Email o contraseña incorrectos.");
            return View();
        }

        //un usuario dado de baja no puede entrar
        if (!usuario.Activo)
        {
            ModelState.AddModelError("", "El usuario está inactivo. Consultá con el administrador.");
            return View();
        }

        await IniciarSesion(usuario);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    //cada usuario edita el suyo

    [Authorize]
    [HttpGet]
    public IActionResult Perfil()
    {
        var id = ObtenerIdUsuarioActual();
        if (id == null) return Challenge();

        var usuario = repositorioUsuario.ObtenerPorId(id.Value);
        if (usuario == null) return NotFound();

        return View(usuario);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(
        string nombre,
        string apellido,
        IFormFile? avatar,              //el archivo de la imagen
        string? passwordActual,
        string? passwordNueva,
        string? confirmarPassword)
    {
        var id = ObtenerIdUsuarioActual();
        if (id == null) return Challenge();

        var usuario = repositorioUsuario.ObtenerPorId(id.Value);
        if (usuario == null) return NotFound();

        // 1. Datos personales
        if (string.IsNullOrWhiteSpace(nombre))
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(apellido))
            ModelState.AddModelError("Apellido", "El apellido es obligatorio.");

        // 2. Avatar (opcional): se valida ANTES de guardar nada
        string? extension = null;
        if (avatar != null && avatar.Length > 0)
        {
            extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
                ModelState.AddModelError("Avatar", "Solo se permiten imágenes JPG o PNG.");
            else if (avatar.Length > TamanioMaximo)
                ModelState.AddModelError("Avatar", "La imagen no puede superar los 2 MB.");
        }

        // 3. Contraseña (opcional): solo si completó algún campo
        var quiereCambiarPassword =
            !string.IsNullOrWhiteSpace(passwordActual) || !string.IsNullOrWhiteSpace(passwordNueva);

        if (quiereCambiarPassword)
        {
            if (string.IsNullOrWhiteSpace(passwordActual)
                || passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, passwordActual)
                   == PasswordVerificationResult.Failed)
                ModelState.AddModelError("PasswordActual", "La contraseña actual es incorrecta.");

            if (string.IsNullOrWhiteSpace(passwordNueva) || passwordNueva.Length < 6)
                ModelState.AddModelError("PasswordNueva", "La nueva contraseña debe tener al menos 6 caracteres.");
            else if (passwordNueva != confirmarPassword)
                ModelState.AddModelError("ConfirmarPassword", "Las contraseñas no coinciden.");
        }

        if (!ModelState.IsValid)
        {
            usuario.Nombre = nombre ?? string.Empty;
            usuario.Apellido = apellido ?? string.Empty;
            return View(usuario);
        }

        // se aplican los cambios
        usuario.Nombre = nombre.Trim();
        usuario.Apellido = apellido.Trim();

        if (quiereCambiarPassword)
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, passwordNueva!);

        if (extension != null)
            usuario.Avatar = await GuardarAvatar(usuario.Id, avatar!, extension, usuario.Avatar);

        repositorioUsuario.Modificacion(usuario);

        await IniciarSesion(usuario);   // renueva la cookie con el nombre y el avatar nuevos

        TempData["Mensaje"] = "Perfil actualizado correctamente.";
        return RedirectToAction(nameof(Perfil));
    }

    // ---------- MÉTODOS AUXILIARES ----------

    private async Task<string> GuardarAvatar(int idUsuario, IFormFile archivo, string extension, string? avatarAnterior)
    {
        var carpeta = Path.Combine(entorno.WebRootPath, "uploads", "avatares");
        Directory.CreateDirectory(carpeta);   // si no existe, la crea

        var nombreArchivo = $"{idUsuario}{extension}";
        var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

        // Si el avatar anterior tenía otra extensión (por ejemplo .png y ahora sube .jpg), se borra
        if (!string.IsNullOrEmpty(avatarAnterior))
        {
            var rutaAnterior = Path.Combine(entorno.WebRootPath, avatarAnterior.TrimStart('/'));
            if (rutaAnterior != rutaCompleta && System.IO.File.Exists(rutaAnterior))
                System.IO.File.Delete(rutaAnterior);
        }

        using var stream = new FileStream(rutaCompleta, FileMode.Create);
        await archivo.CopyToAsync(stream);

        return $"/uploads/avatares/{nombreArchivo}";
    }

    private int? ObtenerIdUsuarioActual()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

//arma la credencial
    private async Task IniciarSesion(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim(ClaimTypes.GivenName, $"{usuario.Nombre} {usuario.Apellido}"),   // NUEVO
            new Claim("Avatar", usuario.Avatar ?? string.Empty)                        // NUEVO
        };

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));
    }
}