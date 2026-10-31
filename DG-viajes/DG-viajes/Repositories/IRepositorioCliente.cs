using DG_viajes.Models;

namespace DG_viajes.Repositories;

public interface IRepositorioCliente : IRepositorio<Cliente>
{
    bool ExisteDni(string dni, int idExcluir = 0 );
}

//al editar un cliente no tiene que encontrarse a uno mismo