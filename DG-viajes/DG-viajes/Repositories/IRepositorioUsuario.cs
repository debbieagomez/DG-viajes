using DG_viajes.Models;

namespace DG_viajes.Repositories;

public interface IRepositorioUsuario : IRepositorio<Usuario>
{
    Usuario? ObtenerPorEmail(string email);
    bool ExisteEmail(string email, int idExcluir = 0);
}