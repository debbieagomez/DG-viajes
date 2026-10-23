namespace DG_viajes.Models;

public class Reserva
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public int PaqueteId { get; set; }

    public int HospedajeId { get; set; }

    public DateTime FechaReserva { get; set; }

    public int CantPasajeros { get; set; }

    public decimal PrecioTotal { get; set; }

    public string Estado { get; set; }= "Pendiente";

    public int CreadoPor { get; set; }

    public DateTime CreadoEn { get; set; }

    public int? ModificadoPor { get; set; }

    public DateTime? ModificadoEn { get; set; }
}

