namespace DG_viajes.ViewModels;

public class HospedajeFiltro
{
    public string? Buscar { get; set; }
    public int? DestinoId { get; set; }
    public int? TipoId { get; set; }
    public string? Estado { get; set; }   // "activos", "inactivos" o vacío (todos)
    public int Pagina { get; set; } = 1;
    public int Tamanio { get; set; } = 10;
}

public record HospedajeFila(
    int Id, int DestinoId, int TipoId, string Nombre, string? Direccion,
    int CapacidadMaxima, string? Servicios, decimal PrecioNoche, bool Activo,
    string Destino, string Tipo, string? Portada);

public record Paginado<T>(List<T> Items, int Total, int Pagina, int Tamanio);
