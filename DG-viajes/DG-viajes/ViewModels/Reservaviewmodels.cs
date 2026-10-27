using DG_viajes.Models;

namespace DG_viajes.ViewModels;

public class ReservaFiltro
{
    public string? Buscar { get; set; }   // DNI, nombre o apellido del cliente
    public string? Estado { get; set; }   // Pendiente, Confirmada, Cancelada o vacío
    public int Pagina { get; set; } = 1;
    public int Tamanio { get; set; } = 10;
}

public record ReservaFila(
    int Id, string Cliente, string Dni, string Paquete, string Destino, string Hospedaje,
    DateTime FechaReserva, int CantPasajeros, decimal PrecioTotal, decimal Pagado, string Estado)
{
    public decimal Saldo => PrecioTotal - Pagado;
}

public class ReservaIndexVm
{
    public ReservaFiltro Filtro { get; set; } = new();
    public Paginado<ReservaFila> Resultado { get; set; } = null!;
}

// Opciones que devuelven las búsquedas AJAX
public record ClienteOpcion(int Id, string Dni, string Nombre, string Apellido);

public record PaqueteOpcion(
    int Id, string Nombre, int DestinoId, string Destino, decimal PrecioPersona,
    DateTime FechaSalida, DateTime FechaRegreso, int Noches, int PlazasLibres);

public record HospedajeOpcion(
    int Id, string Nombre, string Tipo, string? Direccion, int CapacidadMaxima,
    decimal PrecioNoche, int LugaresLibres);

// Lo que envía el formulario de nueva reserva
public class ReservaNueva
{
    public int ClienteId { get; set; }
    public int PaqueteId { get; set; }
    public int HospedajeId { get; set; }
    public int CantPasajeros { get; set; }
    public List<Acompanante> Acompanantes { get; set; } = new();
}

public record ResultadoReserva(List<string> Errores, int Id)
{
    public bool Ok => Errores.Count == 0;
}

public class ReservaDetalle
{
    public Reserva Reserva { get; set; } = new();
    public string ClienteNombre { get; set; } = "";
    public string ClienteDni { get; set; } = "";
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public string Paquete { get; set; } = "";
    public string Destino { get; set; } = "";
    public DateTime FechaSalida { get; set; }
    public DateTime FechaRegreso { get; set; }
    public string Hospedaje { get; set; } = "";
    public string TipoHospedaje { get; set; } = "";
    public decimal Pagado { get; set; }
    public string CreadoPor { get; set; } = "";
    public string? ModificadoPor { get; set; }
    public List<Acompanante> Acompanantes { get; set; } = new();
    public decimal Saldo => Reserva.PrecioTotal - Pagado;
}