using System.Security.Claims;
using System.Text.RegularExpressions;
using DG_viajes.Repositories;
using DG_viajes.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

public class ReservasController : Controller
{
    private readonly RepositorioReserva repo;

    public ReservasController(IConfiguration config)
    {
        repo = new RepositorioReserva(config);
    }

    public IActionResult Index(ReservaFiltro filtro)
        => View(new ReservaIndexVm { Filtro = filtro, Resultado = repo.Listar(filtro) });

    public IActionResult Detalle(int id)
    {
        var d = repo.Detalle(id);
        if (d == null) return NotFound();
        return View(d);
    }

    // ---------- Formulario de nueva reserva (Vue) ----------
    [HttpGet]
    public IActionResult Crear() => View();

    [HttpGet]
    public IActionResult BuscarClientes(string? q) => Json(repo.BuscarClientes(q));

    [HttpGet]
    public IActionResult BuscarPaquetes(string? q) => Json(repo.BuscarPaquetes(q));

    [HttpGet]
    public IActionResult BuscarHospedajes(int paqueteId, string? q) => Json(repo.BuscarHospedajes(paqueteId, q));

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Guardar([FromBody] ReservaNueva? n)
    {
        if (n == null) return BadRequest(new { errores = new[] { "No se recibieron datos." } });

        var errores = ValidarForma(n);
        if (errores.Count > 0) return BadRequest(new { errores });

        // Las reglas que dependen de la base (plazas, capacidad, destino, etc.) se validan dentro de la transacción.
        var resultado = repo.Crear(n, UsuarioActualId());
        if (!resultado.Ok) return BadRequest(new { errores = resultado.Errores });
        return Json(new { ok = true, id = resultado.Id });
    }

    // ---------- Cambios de estado ----------
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Confirmar(int id) => Cambiar(id, "Confirmada", "Reserva confirmada.");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancelar(int id) => Cambiar(id, "Cancelada", "Reserva cancelada.");

    private IActionResult Cambiar(int id, string estado, string mensajeOk)
    {
        var error = repo.CambiarEstado(id, estado, UsuarioActualId());
        if (error == null) TempData["Mensaje"] = mensajeOk;
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- Auxiliares ----------
    // Validaciones que no necesitan la base de datos.
    private static List<string> ValidarForma(ReservaNueva n)
    {
        var e = new List<string>();
        if (n.ClienteId <= 0) e.Add("Elegí un cliente.");
        if (n.PaqueteId <= 0) e.Add("Elegí un paquete.");
        if (n.HospedajeId <= 0) e.Add("Elegí un hospedaje.");
        if (n.CantPasajeros <= 0) e.Add("La cantidad de pasajeros debe ser mayor a cero.");

        n.Acompanantes ??= new();
        if (n.CantPasajeros > 0 && n.Acompanantes.Count != n.CantPasajeros - 1)
            e.Add($"Cargá los datos de {n.CantPasajeros - 1} acompañante(s): son {n.CantPasajeros} pasajeros contando al titular.");

        int fila = 0;
        foreach (var a in n.Acompanantes)
        {
            fila++;
            a.Dni = (a.Dni ?? "").Trim();
            a.Nombre = (a.Nombre ?? "").Trim();
            a.Apellido = (a.Apellido ?? "").Trim();
            a.Telefono = string.IsNullOrWhiteSpace(a.Telefono) ? null : a.Telefono.Trim();

            if (!Regex.IsMatch(a.Dni, @"^\d{6,15}$")) e.Add($"Acompañante {fila}: el DNI debe tener solo números (entre 6 y 15 dígitos).");
            if (a.Nombre.Length == 0 || a.Nombre.Length > 100) e.Add($"Acompañante {fila}: el nombre es obligatorio (hasta 100 caracteres).");
            if (a.Apellido.Length == 0 || a.Apellido.Length > 100) e.Add($"Acompañante {fila}: el apellido es obligatorio (hasta 100 caracteres).");
            if (a.Telefono?.Length > 30) e.Add($"Acompañante {fila}: el teléfono no puede superar los 30 caracteres.");
        }
        if (n.Acompanantes.Select(a => a.Dni).Where(d => d.Length > 0).GroupBy(d => d).Any(g => g.Count() > 1))
            e.Add("No puede haber dos acompañantes con el mismo DNI.");
        return e;
    }

    // TODO: cuando exista el login, quitar el usuario provisorio (1 = administrador de los datos de prueba).
    private int UsuarioActualId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 1;
    }
}