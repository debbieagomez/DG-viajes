using DG_viajes.Models;
using DG_viajes.Repositories;
using DG_viajes.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

public class PaquetesController : Controller
{
    private readonly RepositorioPaquete repo;

    public PaquetesController(IConfiguration config)
    {
        repo = new RepositorioPaquete(config);
    }

    // Los filtros llegan por query string y se aplican en la base de datos.
    public IActionResult Index(PaqueteFiltro filtro)
    {
        var vm = new PaqueteIndexVm
        {
            Filtro = filtro,
            Resultado = repo.Listar(filtro),
            Destinos = repo.Destinos()
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        var p = new Paquete
        {
            FechaSalida = DateTime.Today.AddDays(30),
            FechaRegreso = DateTime.Today.AddDays(37),
            PlazasTotales = 20
        };
        CargarFormulario(null);
        return View("Form", p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Crear(Paquete paquete)
    {
        Validar(paquete, null);
        if (!ModelState.IsValid)
        {
            CargarFormulario(null);
            return View("Form", paquete);
        }
        repo.Alta(paquete);
        TempData["Mensaje"] = "Paquete creado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        var p = repo.Obtener(id);
        if (p == null) return NotFound();
        CargarFormulario(p);
        return View("Form", p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Editar(Paquete paquete)
    {
        var actual = repo.Obtener(paquete.Id);
        if (actual == null) return NotFound();

        Validar(paquete, actual);
        if (!ModelState.IsValid)
        {
            CargarFormulario(actual);
            return View("Form", paquete);
        }
        repo.Modificar(paquete);
        TempData["Mensaje"] = "Cambios guardados.";
        return RedirectToAction(nameof(Index));
    }

    // Suspender o reactivar: no modifica las reservas existentes, solo impide reservas nuevas.
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CambiarEstado(int id, string estado, string? volver)
    {
        if (!RepositorioPaquete.Estados.Contains(estado)) return BadRequest();
        if (repo.Obtener(id) == null) return NotFound();
        repo.CambiarEstado(id, estado);
        TempData["Mensaje"] = estado == "Suspendido" ? "Paquete suspendido." : "Paquete reactivado.";
        return Volver(volver);
    }

    // TODO: restringir a administradores con [Authorize(Roles = "Administrador")] cuando esté el login.
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id, string? volver)
    {
        if (repo.Obtener(id) == null) return NotFound();
        if (repo.TieneReservas(id))
        {
            TempData["Error"] = "El paquete tiene reservas asociadas. Suspendelo en lugar de eliminarlo.";
            return Volver(volver);
        }
        repo.Eliminar(id);
        TempData["Mensaje"] = "Paquete eliminado.";
        return Volver(volver);
    }

    // ---------- Auxiliares ----------
    private IActionResult Volver(string? volver)
    {
        var destino = Url.Action(nameof(Index)) ?? "/Paquetes";
        if (!string.IsNullOrEmpty(volver) && volver.StartsWith("?")) destino += volver;
        return LocalRedirect(destino);
    }

    private void CargarFormulario(Paquete? actual)
    {
        ViewBag.Destinos = repo.Destinos();
        ViewBag.Ocupadas = actual != null ? repo.PlazasOcupadas(actual.Id) : 0;
        ViewBag.TieneReservas = actual != null && repo.TieneReservas(actual.Id);
    }

    private void Validar(Paquete p, Paquete? actual)
    {
        ModelState.Clear(); // se validan a mano, con mensajes en español
        void Err(string mensaje) => ModelState.AddModelError(string.Empty, mensaje);

        p.Nombre = (p.Nombre ?? "").Trim();
        p.Descripcion = string.IsNullOrWhiteSpace(p.Descripcion) ? null : p.Descripcion.Trim();

        if (p.Nombre.Length == 0) Err("El nombre es obligatorio.");
        else if (p.Nombre.Length > 120) Err("El nombre no puede superar los 120 caracteres.");
        if (p.Descripcion?.Length > 4000) Err("La descripción no puede superar los 4000 caracteres.");
        if (p.PrecioPersona <= 0 || p.PrecioPersona > 9999999999m) Err("El precio por persona debe ser mayor a cero.");
        if (p.PlazasTotales <= 0) Err("Las plazas deben ser mayores a cero.");

        if (p.FechaSalida == default || p.FechaRegreso == default)
            Err("Ingresá las fechas de salida y de regreso.");
        else
        {
            if (p.FechaRegreso.Date <= p.FechaSalida.Date)
                Err("La fecha de regreso debe ser posterior a la de salida.");

            // No se permiten salidas en el pasado al crear, ni al cambiar la fecha de un paquete existente.
            bool fechaNueva = actual == null || p.FechaSalida.Date != actual.FechaSalida.Date;
            if (fechaNueva && p.FechaSalida.Date < DateTime.Today)
                Err("La fecha de salida no puede ser anterior a hoy.");
        }

        if (!repo.DestinoPermitido(p.DestinoId, actual?.DestinoId ?? 0)) Err("Elegí un destino activo.");

        if (actual != null)
        {
            // El hospedaje de cada reserva debe ser del destino del paquete: con reservas no se puede cambiar.
            if (p.DestinoId != actual.DestinoId && repo.TieneReservas(p.Id))
                Err("No se puede cambiar el destino: el paquete ya tiene reservas.");
            int ocupadas = repo.PlazasOcupadas(p.Id);
            if (p.PlazasTotales < ocupadas)
                Err($"Las plazas no pueden ser menos que las ya reservadas ({ocupadas}).");
        }
    }
}
