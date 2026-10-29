using DG_viajes.Models;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace DG_viajes.Repositories;

// Acceso a usuarios solo para la Web API. El login web usa el RepositorioUsuario de Deborah.
public class RepositorioUsuarioApi
{
    private readonly string cs;

    public RepositorioUsuarioApi(IConfiguration config)
    {
        cs = config.GetConnectionString("MySqlConnection")
             ?? throw new InvalidOperationException("Falta la cadena de conexión MySqlConnection.");
    }

    public Usuario? ObtenerPorEmail(string email)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT id, email, password_hash, nombre, apellido, avatar, rol, activo
                                           FROM usuario WHERE email = @e", c);
        cmd.Parameters.AddWithValue("@e", email);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        int ia = r.GetOrdinal("avatar");
        return new Usuario
        {
            Id = r.GetInt32("id"), Email = r.GetString("email"), PasswordHash = r.GetString("password_hash"),
            Nombre = r.GetString("nombre"), Apellido = r.GetString("apellido"),
            Avatar = r.IsDBNull(ia) ? null : r.GetString(ia), Rol = r.GetString("rol"), Activo = r.GetBoolean("activo")
        };
    }

    // Solo para desarrollo: reemplaza el PENDIENTE_GENERAR_HASH de los datos de prueba por un hash real.
    // Cada usuario recibe su propio hash (con su propia sal).
    public int InicializarPasswords(Func<string> generarHash)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        var ids = new List<int>();
        using (var q = new MySqlCommand("SELECT id FROM usuario WHERE password_hash = 'PENDIENTE_GENERAR_HASH'", c))
        using (var r = q.ExecuteReader())
            while (r.Read()) ids.Add(r.GetInt32(0));

        foreach (var id in ids)
        {
            using var u = new MySqlCommand("UPDATE usuario SET password_hash = @h WHERE id = @id", c);
            u.Parameters.AddWithValue("@h", generarHash());
            u.Parameters.AddWithValue("@id", id);
            u.ExecuteNonQuery();
        }
        return ids.Count;
    }
}