using DG_viajes.Models;
using DG_viajes.ViewModels;

namespace DG_viajes.Repositories;

public interface IRepositorioPago
{
    ResumenReservaPagos? ObtenerResumen(int reservaId);
    IList<PagoFila> ObtenerPagos(int reservaId);

    // Devuelve la lista de errores de validación; vacía si el pago se registró.
    List<string> Registrar(Pago pago);

    // Devuelve null si se anuló, o el mensaje de error.
    string? Anular(int pagoId, int usuarioId);
}