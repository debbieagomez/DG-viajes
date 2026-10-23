namespace DG_viajes.Models;

public class Pago
{
    public int Id { get; set; }

    public int ReservaId { get; set; }

    public string Tipo { get; set; }= string.Empty;

    public DateTime Fecha { get; set; }

    public decimal Importe { get; set; }

    public string MedioPago { get; set; }= string.Empty;

    public int CreadoPor { get; set; }

    public DateTime CreadoEn { get; set; }

    public int? AnuladoPor { get; set; }

    public DateTime? AnuladoEn { get; set; }

}
