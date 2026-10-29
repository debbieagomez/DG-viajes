using DG_viajes.Repositories;
using DG_viajes.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReservasApiController : ControllerBase
{
    private readonly RepositorioReserva repo;

    public ReservasApiController(IConfiguration config)
    {
        repo = new RepositorioReserva(config);
    }

    // GET /api/reservas?buscar=&estado=Confirmada&pagina=1&tamanio=10
    [HttpGet]
    public IActionResult Listar([FromQuery] ReservaFiltro filtro) => Ok(repo.Listar(filtro));

    // GET /api/reservas/1  (la auditoría solo la ven los administradores)
    [HttpGet("{id:int}")]
    public IActionResult Obtener(int id)
    {
        var d = repo.Detalle(id);
        if (d == null) return NotFound(new { error = "La reserva no existe." });

        bool esAdmin = User.IsInRole("Administrador");
        return Ok(new
        {
            d.Reserva.Id,
            cliente = new { nombre = d.ClienteNombre, dni = d.ClienteDni, email = d.ClienteEmail, telefono = d.ClienteTelefono },
            viaje = new { paquete = d.Paquete, destino = d.Destino, d.FechaSalida, d.FechaRegreso, hospedaje = d.Hospedaje, tipoHospedaje = d.TipoHospedaje },
            d.Reserva.FechaReserva,
            d.Reserva.CantPasajeros,
            d.Reserva.PrecioTotal,
            pagado = d.Pagado,
            saldo = d.Reserva.Estado == "Cancelada" ? 0 : d.Saldo,
            d.Reserva.Estado,
            acompanantes = d.Acompanantes.Select(a => new { a.Dni, a.Nombre, a.Apellido, a.Telefono }),
            auditoria = esAdmin
                ? new { creadoPor = d.CreadoPor, d.Reserva.CreadoEn, modificadoPor = d.ModificadoPor, d.Reserva.ModificadoEn }
                : null
        });
    }
}