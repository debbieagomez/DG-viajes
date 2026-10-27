namespace DG_viajes.Repositories;

public interface IRepositorio<T>
{
    
    int Alta(T entidad);
    int Baja(int Id);
    int Modificacion(T entidad);
    IList<T> ObtenerLista(string? busqueda = null, int pagina = 1, int tamPagina = 10);
    int ObtenerCantidad(string? busqueda = null);
    T? ObtenerPorId(int id);


}