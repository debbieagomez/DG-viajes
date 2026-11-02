using DG_viajes.Models;

namespace DG_viajes.Repositories;

public interface IRepositorioTipoHospedaje : IRepositorio<TipoHospedaje>
{
    bool ExisteNombre(string nombre, int idExcluir = 0);
}