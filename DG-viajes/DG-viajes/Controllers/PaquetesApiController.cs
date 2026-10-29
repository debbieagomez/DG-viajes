using DG_viajes.Repositories;
using DG_viajes.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

[ApiController]
[Route("api/paquetes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaquetesApiController : ControllerBase
{
    private readonly RepositorioPaquete repo;

    public PaquetesApiController(IConfiguration config)
    {
        repo = new RepositorioPaquete(config);
    }

    // GET /api/paquetes?destinoId=&precioMin=&precioMax=&salidaDesde=2027-01-01&salidaHasta=&estado=&pagina=1&tamanio=10
    [HttpGet]
    public IActionResult Listar([FromQuery] PaqueteFiltro filtro) => Ok(repo.Listar(filtro));

    // GET /api/paquetes/3
    [HttpGet("{id:int}")]
    public IActionResult Obtener(int id)
    {
        var p = repo.Obtener(id);
        if (p == null) return NotFound(new { error = "El paquete no existe." });
        int ocupadas = repo.PlazasOcupadas(id);
        return Ok(new
        {
            p.Id, p.DestinoId, p.Nombre, p.Descripcion, p.PrecioPersona, p.FechaSalida, p.FechaRegreso,
            p.PlazasTotales, plazasOcupadas = ocupadas, plazasDisponibles = p.PlazasTotales - ocupadas,
            p.IncluyeTransporte, p.Estado
        });
    }
}