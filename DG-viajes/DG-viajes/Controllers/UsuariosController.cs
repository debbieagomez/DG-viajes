using System.Net.Mail;
using System.Security.Claims;
using DG_viajes.Models;
using DG_viajes.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

// Solo el administrador gestiona las cuentas de los demás usuarios.
// El listado, la búsqueda y la baja vienen del ABMController; Crear y Editar agregan lo propio de una cuenta
// (email único, rol, contraseña con hash). "Eliminar" es una baja lógica: el usuario queda inactivo.
[Authorize(Roles = "Administrador")]
public class UsuariosController : ABMController<Usuario>
{
    private static readonly string[] Roles = { "Administrador", "Empleado" };

    private readonly IRepositorioUsuario repo;
    private readonly IPasswordHasher<Usuario> hasher;

    public UsuariosController(IRepositorioUsuario repositorio, IPasswordHasher<Usuario> hasher) : base(repositorio)
    {
        repo = repositorio;
        this.hasher = hasher;
    }

    // ---------- Crear ----------
    [HttpGet]
    public override IActionResult Crear()
    {
        return View(new Usuario { Activo = true, Rol = "Empleado" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public override IActionResult Crear(Usuario entidad)
    {
        ModelState.Remove(nameof(Usuario.PasswordHash)); // el formulario manda "Password", no el hash
        ValidarDatos(entidad, 0);

        var password = Request.Form["Password"].ToString();
        ValidarPassword(password, Request.Form["ConfirmarPassword"].ToString(), obligatoria: true);

        entidad.Avatar = null; // el avatar lo carga cada usuario desde su perfil
        if (ModelState.IsValid) entidad.PasswordHash = hasher.HashPassword(entidad, password);

        return base.Crear(entidad);
    }

    // ---------- Editar ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public override IActionResult Editar(Usuario entidad)
    {
        var actual = repo.ObtenerPorId(entidad.Id);
        if (actual == null) return NotFound();

        ModelState.Remove(nameof(Usuario.PasswordHash));
        ValidarDatos(entidad, entidad.Id);

        // El administrador no puede dejarse afuera a sí mismo (así nunca queda el sistema sin administrador activo).
        if (entidad.Id == IdUsuarioActual())
        {
            if (!entidad.Activo) ModelState.AddModelError("Activo", "No podés desactivar tu propia cuenta.");
            if (entidad.Rol != "Administrador") ModelState.AddModelError("Rol", "No podés quitarte el rol de administrador.");
        }

        // Contraseña vacía = se conserva la actual.
        var password = Request.Form["Password"].ToString();
        ValidarPassword(password, Request.Form["ConfirmarPassword"].ToString(), obligatoria: false);
        entidad.PasswordHash = ModelState.IsValid && !string.IsNullOrEmpty(password)
            ? hasher.HashPassword(entidad, password)
            : actual.PasswordHash;

        // La modificación del repositorio reescribe el avatar: se conserva el que ya tenía.
        entidad.Avatar = actual.Avatar;

        return base.Editar(entidad);
    }

    // ---------- Baja lógica ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public override IActionResult EliminarConfirmado(int id)
    {
        if (id == IdUsuarioActual())
        {
            MostrarError("No podés desactivar tu propia cuenta.");
            return RedirectToAction(nameof(Index));
        }
        return base.EliminarConfirmado(id);
    }

    // ---------- Auxiliares ----------
    private void ValidarDatos(Usuario u, int idExcluir)
    {
        u.Email = (u.Email ?? "").Trim();
        u.Nombre = (u.Nombre ?? "").Trim();
        u.Apellido = (u.Apellido ?? "").Trim();

        // Se reemplazan los mensajes automáticos (en inglés) por los propios.
        void Error(string campo, string mensaje)
        {
            ModelState.Remove(campo);
            ModelState.AddModelError(campo, mensaje);
        }

        if (u.Email.Length == 0) Error(nameof(Usuario.Email), "El email es obligatorio.");
        else if (u.Email.Length > 150) Error(nameof(Usuario.Email), "El email no puede superar los 150 caracteres.");
        else if (!MailAddress.TryCreate(u.Email, out var m) || m.Address != u.Email) Error(nameof(Usuario.Email), "El email no es válido.");
        else if (repo.ExisteEmail(u.Email, idExcluir)) Error(nameof(Usuario.Email), "Ese email ya está registrado.");

        if (u.Nombre.Length == 0) Error(nameof(Usuario.Nombre), "El nombre es obligatorio.");
        else if (u.Nombre.Length > 100) Error(nameof(Usuario.Nombre), "El nombre no puede superar los 100 caracteres.");

        if (u.Apellido.Length == 0) Error(nameof(Usuario.Apellido), "El apellido es obligatorio.");
        else if (u.Apellido.Length > 100) Error(nameof(Usuario.Apellido), "El apellido no puede superar los 100 caracteres.");

        if (!Roles.Contains(u.Rol)) Error(nameof(Usuario.Rol), "Elegí un rol válido.");
    }

    private void ValidarPassword(string password, string confirmar, bool obligatoria)
    {
        if (string.IsNullOrEmpty(password))
        {
            if (obligatoria) ModelState.AddModelError("Password", "La contraseña es obligatoria.");
            return;
        }
        if (password.Length < 6) ModelState.AddModelError("Password", "La contraseña debe tener al menos 6 caracteres.");
        else if (password != confirmar) ModelState.AddModelError("ConfirmarPassword", "Las contraseñas no coinciden.");
    }

    private int IdUsuarioActual()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }
}