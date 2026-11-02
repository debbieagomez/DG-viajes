using DG_viajes.Models;
using MySqlConnector;

namespace DG_viajes.Repositories;

public class RepositorioTipoHospedaje : IRepositorioTipoHospedaje
{
    private readonly string connectionString;

    public RepositorioTipoHospedaje(string connectionString)
    {
        this.connectionString = connectionString;
    }

    // Lee la fila una sola vez.
    private static TipoHospedaje Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        Nombre = r.GetString("nombre")
    };

    private static string ParametroBusqueda(string? busqueda) => "%" + (busqueda ?? "").Trim() + "%";

    public int Alta(TipoHospedaje entidad)
    {
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand("INSERT INTO tipo_hospedaje (nombre) VALUES (@nombre)", c);
        cmd.Parameters.AddWithValue("@nombre", entidad.Nombre);
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    // La tabla no tiene "activo": la baja es un DELETE. Con hospedajes asociados MySQL responde 1451.
    public int Baja(int id)
    {
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand("DELETE FROM tipo_hospedaje WHERE id = @id", c);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery();
    }

    public int Modificacion(TipoHospedaje entidad)
    {
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand("UPDATE tipo_hospedaje SET nombre = @nombre WHERE id = @id", c);
        cmd.Parameters.AddWithValue("@nombre", entidad.Nombre);
        cmd.Parameters.AddWithValue("@id", entidad.Id);
        return cmd.ExecuteNonQuery();
    }

    public IList<TipoHospedaje> ObtenerLista(string? busqueda = null, int pagina = 1, int tamPagina = 10)
    {
        var lista = new List<TipoHospedaje>();
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT id, nombre FROM tipo_hospedaje
            WHERE nombre LIKE @b ORDER BY nombre LIMIT @tam OFFSET @off", c);
        cmd.Parameters.AddWithValue("@b", ParametroBusqueda(busqueda));
        cmd.Parameters.AddWithValue("@tam", tamPagina);
        cmd.Parameters.AddWithValue("@off", (pagina - 1) * tamPagina);
        using var r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(Mapear(r));
        return lista;
    }

    public int ObtenerCantidad(string? busqueda = null)
    {
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM tipo_hospedaje WHERE nombre LIKE @b", c);
        cmd.Parameters.AddWithValue("@b", ParametroBusqueda(busqueda));
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public TipoHospedaje? ObtenerPorId(int id)
    {
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand("SELECT id, nombre FROM tipo_hospedaje WHERE id = @id", c);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    public bool ExisteNombre(string nombre, int idExcluir = 0)
    {
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM tipo_hospedaje WHERE nombre = @n AND id <> @id", c);
        cmd.Parameters.AddWithValue("@n", nombre);
        cmd.Parameters.AddWithValue("@id", idExcluir);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}