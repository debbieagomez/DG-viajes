using DG_viajes.Models;
using MySqlConnector;

namespace DG_viajes.Repositories;

public class RepositorioCliente : IRepositorioCliente
{
    private readonly string connectionString;

    private const string Columnas = "id, dni, nombre, apellido, email, telefono, domicilio, activo";

    public RepositorioCliente(string connectionString)
    {
        this.connectionString = connectionString;
    }

    
    private static Cliente Mapear(MySqlDataReader reader)
    {
        return new Cliente
        {
            Id = reader.GetInt32("id"),
            Dni = reader.GetString("dni"),
            Nombre = reader.GetString("nombre"),
            Apellido = reader.GetString("apellido"),
            Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
            Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString("telefono"),
            Domicilio = reader.IsDBNull(reader.GetOrdinal("domicilio")) ? null : reader.GetString("domicilio"),
            Activo = reader.GetBoolean("activo")
        };
    }


    private static object ParametroBusqueda(string? busqueda)
    {
        return string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : (object)("%" + busqueda.Trim() + "%");
    }

    public Cliente? ObtenerPorId(int id)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = $"SELECT {Columnas} FROM cliente WHERE id = @id;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        using var reader = comando.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public bool ExisteDni(string dni, int idExcluir = 0)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = "SELECT COUNT(*) FROM cliente WHERE dni = @dni AND id <> @idExcluir;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@dni", dni);
        comando.Parameters.AddWithValue("@idExcluir", idExcluir);
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    public IList<Cliente> ObtenerLista(string? busqueda = null, int pagina = 1, int tamPagina = 10)
    {
        var lista = new List<Cliente>();
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = $@"SELECT {Columnas} FROM cliente
                     WHERE @busqueda IS NULL OR dni LIKE @busqueda OR nombre LIKE @busqueda OR apellido LIKE @busqueda OR email LIKE @busqueda
                     ORDER BY apellido, nombre
                     LIMIT @tamPagina OFFSET @offset;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@busqueda", ParametroBusqueda(busqueda));
        comando.Parameters.AddWithValue("@tamPagina", tamPagina);
        comando.Parameters.AddWithValue("@offset", (pagina - 1) * tamPagina);
        using var reader = comando.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(Mapear(reader));
        }
        return lista;
    }

    public int ObtenerCantidad(string? busqueda = null)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = @"SELECT COUNT(*) FROM cliente
                   WHERE @busqueda IS NULL OR dni LIKE @busqueda OR nombre LIKE @busqueda OR apellido LIKE @busqueda OR email LIKE @busqueda;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@busqueda", ParametroBusqueda(busqueda));
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    
    public int Alta(Cliente  cliente)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = @"INSERT INTO cliente (dni, nombre, apellido, email, telefono, domicilio, activo)
                    VALUES (@dni, @nombre, @apellido, @email, @telefono, @domicilio, @activo);
                    SELECT LAST_INSERT_ID();";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@dni", cliente.Dni);
         comando.Parameters.AddWithValue("@nombre", cliente.Nombre);
        comando.Parameters.AddWithValue("@apellido", cliente.Apellido);
        comando.Parameters.AddWithValue("@email", cliente.Email ?? (object)DBNull.Value ); 
        comando.Parameters.AddWithValue("@telefono", cliente.Telefono ?? (object)DBNull.Value);
        comando.Parameters.AddWithValue("@domicilio", cliente.Domicilio ?? (object)DBNull.Value);
        comando.Parameters.AddWithValue("@activo", cliente.Activo);
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public int Modificacion(Cliente  cliente)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = @"UPDATE cliente SET dni = @dni, nombre = @nombre,
                    apellido = @apellido, email = @email, telefono = @telefono, domicilio = @domicilio, activo = @activo
                    WHERE id = @id;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", cliente.Id);
        comando.Parameters.AddWithValue("@dni", cliente.Dni);
         comando.Parameters.AddWithValue("@nombre", cliente.Nombre);
        comando.Parameters.AddWithValue("@apellido", cliente.Apellido);
        comando.Parameters.AddWithValue("@email", cliente.Email ?? (object)DBNull.Value);
        comando.Parameters.AddWithValue("@telefono", cliente.Telefono ?? (object)DBNull.Value);
        comando.Parameters.AddWithValue("@domicilio", cliente.Domicilio ?? (object)DBNull.Value);
        comando.Parameters.AddWithValue("@activo", cliente.Activo);
        return comando.ExecuteNonQuery();
    }

    // Baja lógica:
    public int Baja(int id)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = "UPDATE cliente SET activo = 0 WHERE id = @id;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        return comando.ExecuteNonQuery();
    }
}