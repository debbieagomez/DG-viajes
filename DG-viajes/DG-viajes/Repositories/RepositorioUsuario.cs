using DG_viajes.Models;
using MySqlConnector;

namespace DG_viajes.Repositories;

public class RepositorioUsuario : IRepositorioUsuario
{
    private readonly string connectionString;

    private const string Columnas = "id, email, password_hash, nombre, apellido, avatar, rol, activo";

    public RepositorioUsuario(string connectionString)
    {
        this.connectionString = connectionString;
    }

    // Convierte la fila actual del reader en un Usuario.
    private static Usuario Mapear(MySqlDataReader reader)
    {
        return new Usuario
        {
            Id = reader.GetInt32("id"),
            Email = reader.GetString("email"),
            PasswordHash = reader.GetString("password_hash"),
            Nombre = reader.GetString("nombre"),
            Apellido = reader.GetString("apellido"),
            Avatar = reader.IsDBNull(reader.GetOrdinal("avatar")) ? null : reader.GetString("avatar"),
            Rol = reader.GetString("rol"),
            Activo = reader.GetBoolean("activo")
        };
    }

    // Si no hay búsqueda, manda NULL; si hay, la envuelve en % para el LIKE.
    private static object ParametroBusqueda(string? busqueda)
    {
        return string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : (object)("%" + busqueda.Trim() + "%");
    }

    public Usuario? ObtenerPorEmail(string email)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = $"SELECT {Columnas} FROM usuario WHERE email = @email;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@email", email);
        using var reader = comando.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public Usuario? ObtenerPorId(int id)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = $"SELECT {Columnas} FROM usuario WHERE id = @id;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        using var reader = comando.ExecuteReader();
        return reader.Read() ? Mapear(reader) : null;
    }

    public bool ExisteEmail(string email, int idExcluir = 0)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = "SELECT COUNT(*) FROM usuario WHERE email = @email AND id <> @idExcluir;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@email", email);
        comando.Parameters.AddWithValue("@idExcluir", idExcluir);
        return Convert.ToInt32(comando.ExecuteScalar()) > 0;
    }

    public IList<Usuario> ObtenerLista(string? busqueda = null, int pagina = 1, int tamPagina = 10)
    {
        var lista = new List<Usuario>();
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = $@"SELECT {Columnas} FROM usuario
                     WHERE @busqueda IS NULL OR nombre LIKE @busqueda OR apellido LIKE @busqueda OR email LIKE @busqueda
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
        var sql = @"SELECT COUNT(*) FROM usuario
                    WHERE @busqueda IS NULL OR nombre LIKE @busqueda OR apellido LIKE @busqueda OR email LIKE @busqueda;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@busqueda", ParametroBusqueda(busqueda));
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    // Devuelve el Id del usuario nuevo (sirve para nombrar el archivo del avatar).
    public int Alta(Usuario usuario)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = @"INSERT INTO usuario (email, password_hash, nombre, apellido, avatar, rol, activo)
                    VALUES (@email, @passwordHash, @nombre, @apellido, @avatar, @rol, @activo);
                    SELECT LAST_INSERT_ID();";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@email", usuario.Email);
        comando.Parameters.AddWithValue("@passwordHash", usuario.PasswordHash);
        comando.Parameters.AddWithValue("@nombre", usuario.Nombre);
        comando.Parameters.AddWithValue("@apellido", usuario.Apellido);
        comando.Parameters.AddWithValue("@avatar", usuario.Avatar ?? (object)DBNull.Value);
        comando.Parameters.AddWithValue("@rol", usuario.Rol);
        comando.Parameters.AddWithValue("@activo", usuario.Activo);
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public int Modificacion(Usuario usuario)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = @"UPDATE usuario SET email = @email, password_hash = @passwordHash, nombre = @nombre,
                    apellido = @apellido, avatar = @avatar, rol = @rol, activo = @activo
                    WHERE id = @id;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", usuario.Id);
        comando.Parameters.AddWithValue("@email", usuario.Email);
        comando.Parameters.AddWithValue("@passwordHash", usuario.PasswordHash);
        comando.Parameters.AddWithValue("@nombre", usuario.Nombre);
        comando.Parameters.AddWithValue("@apellido", usuario.Apellido);
        comando.Parameters.AddWithValue("@avatar", usuario.Avatar ?? (object)DBNull.Value);
        comando.Parameters.AddWithValue("@rol", usuario.Rol);
        comando.Parameters.AddWithValue("@activo", usuario.Activo);
        return comando.ExecuteNonQuery();
    }

    // Baja lógica: el usuario queda inactivo y ya no puede iniciar sesión.
    public int Baja(int id)
    {
        using var conexion = new MySqlConnection(connectionString);
        conexion.Open();
        var sql = "UPDATE usuario SET activo = 0 WHERE id = @id;";
        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        return comando.ExecuteNonQuery();
    }
}