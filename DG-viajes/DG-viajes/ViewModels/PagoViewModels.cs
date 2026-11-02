using DG_viajes.Models;

namespace DG_viajes.ViewModels;

public class ResumenReservaPagos
{
    public int ReservaId { get; set; }
    public string Cliente { get; set; } = "";
    public string Dni { get; set; } = "";
    public string Paquete { get; set; } = "";
    public string Estado { get; set; } = "";
    public decimal PrecioTotal { get; set; }
    public decimal Pagado { get; set; }          // suma de pagos NO anulados
    public decimal Saldo => PrecioTotal - Pagado;
}

public record PagoFila(
    int Id, string Tipo, DateTime Fecha, decimal Importe, string MedioPago,
    string CreadoPor, DateTime CreadoEn, string? AnuladoPor, DateTime? AnuladoEn)
{
    public bool Anulado => AnuladoEn.HasValue;
}

public class PagosVm
{
    public static readonly string[] Tipos = { "Seña", "Parcial", "Total" };
    public static readonly string[] Medios = { "Efectivo", "Débito", "Crédito", "Transferencia" };

    public ResumenReservaPagos Resumen { get; set; } = new();
    public IList<PagoFila> Pagos { get; set; } = new List<PagoFila>();
    public Pago Nuevo { get; set; } = new();

    // Una reserva cancelada no recibe pagos, y sin saldo no hay nada que cobrar.
    public bool PuedeRegistrar => Resumen.Estado != "Cancelada" && Resumen.Saldo > 0;
}