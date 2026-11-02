using System.Security.Claims;
using DG_viajes.Models;
using DG_viajes.Repositories;
using DG_viajes.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DG_viajes.Controllers;

[Authorize] 
public class PagosController : Controller
{
    private readonly IRepositorioPago repositorio;

    public PagosController(IRepositorioPago repositorio)
    {
        this.repositorio = repositorio;
    }

    // Pagos de una reserva, con total pagado, saldo y formulario para registrar uno nuevo.
    public IActionResult Index(int reservaId = 0)
    {
        if (reservaId <= 0)
        {
            TempData["Mensaje"] = "Elegí una reserva para ver o registrar sus pagos.";
            return RedirectToAction("Index", "Reservas");
        }
        var vm = Armar(reservaId, null);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Registrar([Bind("ReservaId,Tipo,Fecha,Importe,MedioPago")] Pago pago)
    {
        ModelState.Clear(); // se validan a mano, con mensajes en español
        void Err(string mensaje) => ModelState.AddModelError(string.Empty, mensaje);

        if (!PagosVm.Tipos.Contains(pago.Tipo)) Err("Elegí el tipo de pago.");
        if (!PagosVm.Medios.Contains(pago.MedioPago)) Err("Elegí el medio de pago.");
        if (pago.Fecha == default) Err("Ingresá la fecha del pago.");
        if (pago.Importe <= 0) Err("El importe debe ser mayor a cero.");

        if (ModelState.IsValid)
        {
            pago.CreadoPor = UsuarioActualId();
            foreach (var e in repositorio.Registrar(pago)) Err(e); // saldo, reserva cancelada, etc.
        }

        if (ModelState.IsValid)
        {
            TempData["Mensaje"] = "Pago registrado.";
            return RedirectToAction(nameof(Index), new { reservaId = pago.ReservaId });
        }

        var vm = Armar(pago.ReservaId, pago);
        if (vm == null) return NotFound();
        return View("Index", vm);
    }

    // TODO: definir si anular queda solo para Administrador ([Authorize(Roles = "Administrador")]).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Anular(int id, int reservaId)
    {
        var error = repositorio.Anular(id, UsuarioActualId());
        if (error == null) TempData["Mensaje"] = "Pago anulado.";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(Index), new { reservaId });
    }

    private PagosVm? Armar(int reservaId, Pago? nuevo)
    {
        var resumen = repositorio.ObtenerResumen(reservaId);
        if (resumen == null) return null;
        return new PagosVm
        {
            Resumen = resumen,
            Pagos = repositorio.ObtenerPagos(reservaId),
            Nuevo = nuevo ?? new Pago { ReservaId = reservaId, Fecha = DateTime.Today, Tipo = "Seña" }
        };
    }

    // TODO: cuando suba el login, quitar el usuario provisorio (2 = empleado de prueba).
    private int UsuarioActualId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 2;
    }
}