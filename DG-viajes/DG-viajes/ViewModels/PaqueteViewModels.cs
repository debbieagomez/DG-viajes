using DG_viajes.Models;

namespace DG_viajes.ViewModels;

public class PaqueteFiltro
{
    public int? DestinoId { get; set; }
    public int? PrecioMin { get; set; }          // en pesos enteros
    public int? PrecioMax { get; set; }
    public DateTime? SalidaDesde { get; set; }   // filtra por fecha de salida
    public DateTime? SalidaHasta { get; set; }
    public string? Estado { get; set; }          // "Disponible", "Suspendido" o vacío
    public int Pagina { get; set; } = 1;
    public int Tamanio { get; set; } = 10;
}

public record PaqueteFila(
    int Id, int DestinoId, string Destino, string Nombre, decimal PrecioPersona,
    DateTime FechaSalida, DateTime FechaRegreso, int PlazasTotales, int PlazasOcupadas,
    bool IncluyeTransporte, string Estado)
{
    public int PlazasDisponibles => PlazasTotales - PlazasOcupadas;
}

public class PaqueteIndexVm
{
    public PaqueteFiltro Filtro { get; set; } = new();
    public Paginado<PaqueteFila> Resultado { get; set; } = null!;
    public List<Destino> Destinos { get; set; } = new();
}
