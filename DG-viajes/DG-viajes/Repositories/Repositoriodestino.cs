
using DG_viajes.Models;
using MySqlConnector;

namespace DG_viajes.Repositories;


public class RepositorioDestino : IRepositorioDestino
{

    private readonly string connectionString;

    public RepositorioDestino(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public int Alta(Destino destino)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = @"INSERT INTO destino( pais, provincia, ciudad, descripcion, activo) VALUES(@pais, @provincia, @ciudad, @descripcion, @activo);";

        using var comando = new MySqlCommand(sql, conexion);

        comando.Parameters.AddWithValue("@pais", destino.Pais);
        comando.Parameters.AddWithValue("@provincia", destino.Provincia ??(object)DBNull.Value );
        comando.Parameters.AddWithValue("@ciudad", destino.Ciudad);
        comando.Parameters.AddWithValue("@descripcion", destino.Descripcion ??(object)DBNull.Value );
        comando.Parameters.AddWithValue("@activo", destino.Activo);
        
       return comando.ExecuteNonQuery();
    }

    public int Baja(int Id)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();

        var sql =@"UPDATE destino SET activo = 0 WHERE id = @id;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", Id);
            
         return comando.ExecuteNonQuery();
    }

    public int Modificacion(Destino destino)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();

        var sql =@"UPDATE destino SET Pais= @pais, Provincia= @provincia, Ciudad= @ciudad, Descripcion= @descripcion, Activo =@activo WHERE Id= @id; ";
        using var comando = new MySqlCommand(sql, conexion);

        comando.Parameters.AddWithValue("@id", destino.Id);
        comando.Parameters.AddWithValue("@pais", destino.Pais);
        comando.Parameters.AddWithValue("@provincia", destino.Provincia ??(object)DBNull.Value );
        comando.Parameters.AddWithValue("@ciudad", destino.Ciudad);
        comando.Parameters.AddWithValue("@descripcion", destino.Descripcion ??(object)DBNull.Value);
        comando.Parameters.AddWithValue("@activo", destino.Activo);

        return comando.ExecuteNonQuery();
    }

    public int ObtenerCantidad(string? busqueda = null)
    {
        using var conexion = new MySqlConnection(connectionString);

        conexion.Open();
        var sql = @"SELECT COUNT(*) FROM destino WHERE @busqueda IS NULL OR @busqueda = '' OR ciudad LIKE @busqueda OR pais LIKE @busqueda; ";
        using var comando = new MySqlCommand(sql, conexion);

          if (string.IsNullOrWhiteSpace(busqueda))
        {
            comando.Parameters.AddWithValue("@busqueda", DBNull.Value);
        }
        else
        {
            comando.Parameters.AddWithValue("@busqueda", "%" + busqueda + "%");
        }

        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public IList<Destino> ObtenerLista(string? busqueda = null, int pagina = 1, int tamPagina = 10)
    {
        var lista = new List<Destino>();
        var offset = (pagina - 1) * tamPagina;
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();

        var sql = @"SELECT id, pais, provincia, ciudad, descripcion, activo FROM destino WHERE @busqueda IS NULL OR @busqueda = '' OR ciudad LIKE OR pais LIKE @busqueda ORDER BY pais, ciudad LIMIT @tamPagina OFFSET @offset; ";
       using var comando = new MySqlCommand(sql, conexion);

       comando.Parameters.AddWithValue("@tamPagina", tamPagina);
       comando.Parameters.AddWithValue("@offset", offset);

       
         if (string.IsNullOrWhiteSpace(busqueda))
        {
            comando.Parameters.AddWithValue("@busqueda", DBNull.Value);
        }
        else
        {
            comando.Parameters.AddWithValue("@busqueda", "%" + busqueda + "%");
        }

       using var reader = comando.ExecuteReader();


        while (reader.Read() )
        {
            var destino = new Destino();
            destino.Id = reader.GetInt32("id");
            destino.Pais = reader.GetString("pais");
            destino.Provincia = reader.IsDBNull(reader.GetOrdinal("provincia")) ? null : reader.GetString("provincia");
            destino.Ciudad = reader.GetString("ciudad");
            destino.Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString("descripcion");
            destino.Activo = reader.GetBoolean("activo");
            lista.Add(destino);
        }
        return lista;
    }

    public Destino? ObtenerPorId(int id)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();

        var sql = @"SELECT id, pais, provincia, ciudad, descripcion, activo FROM destino WHERE id= @id;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);

        using var reader = comando.ExecuteReader();

        if (reader.Read())
        {
            var destino = new Destino();
            destino.Id = reader.GetInt32("id");
            destino.Pais = reader.GetString("pais");
            destino.Provincia = reader.IsDBNull(reader.GetOrdinal("provincia")) ? null : reader.GetString("provincia");
            destino.Ciudad = reader.GetString("ciudad");
            destino.Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? null : reader.GetString("descripcion");
            destino.Activo = reader.GetBoolean("activo");
            return destino;
        }

        return null;
    }
}
