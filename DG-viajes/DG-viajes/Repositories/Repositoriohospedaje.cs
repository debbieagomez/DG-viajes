using DG_viajes.Models;
using DG_viajes.ViewModels;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace DG_viajes.Repositories;

public class RepositorioHospedaje
{
    private readonly string cs;

    public RepositorioHospedaje(IConfiguration config)
    {
        cs = config.GetConnectionString("MySqlConnection")
             ?? throw new InvalidOperationException("Falta la cadena de conexión MySqlConnection.");
    }

    private static string? Txt(MySqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    // ---------- Listado con filtros combinables y paginado por servidor ----------
    public Paginado<HospedajeFila> Listar(HospedajeFiltro f)
    {
        int tam = Math.Clamp(f.Tamanio, 1, 50);
        int pag = Math.Max(f.Pagina, 1);

        var cond = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.Buscar))
            cond.Add("(h.nombre LIKE @q OR h.direccion LIKE @q OR h.servicios LIKE @q)");
        if (f.DestinoId.HasValue) cond.Add("h.destino_id = @dest");
        if (f.TipoId.HasValue) cond.Add("h.tipo_id = @tipo");
        if (f.Estado == "activos") cond.Add("h.activo = 1");
        else if (f.Estado == "inactivos") cond.Add("h.activo = 0");

        string where = cond.Count > 0 ? "WHERE " + string.Join(" AND ", cond) : "";
        const string from = "FROM hospedaje h JOIN destino d ON d.id = h.destino_id JOIN tipo_hospedaje t ON t.id = h.tipo_id";

        void Params(MySqlCommand cmd)
        {
            if (!string.IsNullOrWhiteSpace(f.Buscar)) cmd.Parameters.AddWithValue("@q", "%" + f.Buscar.Trim() + "%");
            if (f.DestinoId.HasValue) cmd.Parameters.AddWithValue("@dest", f.DestinoId.Value);
            if (f.TipoId.HasValue) cmd.Parameters.AddWithValue("@tipo", f.TipoId.Value);
        }

        using var c = new MySqlConnection(cs);
        c.Open();

        int total;
        using (var cc = new MySqlCommand($"SELECT COUNT(*) {from} {where}", c))
        {
            Params(cc);
            total = Convert.ToInt32(cc.ExecuteScalar());
        }

        var items = new List<HospedajeFila>();
        string sql = $@"SELECT h.id, h.destino_id, h.tipo_id, h.nombre, h.direccion, h.capacidad_maxima,
                               h.servicios, h.precio_noche, h.activo, d.ciudad AS destino, t.nombre AS tipo,
                               (SELECT f.archivo FROM foto_hospedaje f WHERE f.hospedaje_id = h.id
                                ORDER BY f.es_portada DESC, f.id LIMIT 1) AS portada
                        {from} {where}
                        ORDER BY h.nombre LIMIT @tam OFFSET @off";
        using (var cmd = new MySqlCommand(sql, c))
        {
            Params(cmd);
            cmd.Parameters.AddWithValue("@tam", tam);
            cmd.Parameters.AddWithValue("@off", (pag - 1) * tam);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                items.Add(new HospedajeFila(
                    r.GetInt32("id"), r.GetInt32("destino_id"), r.GetInt32("tipo_id"), r.GetString("nombre"),
                    Txt(r, "direccion"), r.GetInt32("capacidad_maxima"), Txt(r, "servicios"),
                    r.GetDecimal("precio_noche"), r.GetBoolean("activo"),
                    r.GetString("destino"), r.GetString("tipo"), Txt(r, "portada")));
            }
        }
        return new Paginado<HospedajeFila>(items, total, pag, tam);
    }

    // ---------- Listas para los selects ----------
    public List<Destino> Destinos()
    {
        var lista = new List<Destino>();
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("SELECT id, pais, provincia, ciudad, descripcion, activo FROM destino ORDER BY ciudad", c);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new Destino
            {
                Id = r.GetInt32("id"), Pais = r.GetString("pais"), Provincia = Txt(r, "provincia"),
                Ciudad = r.GetString("ciudad"), Descripcion = Txt(r, "descripcion"), Activo = r.GetBoolean("activo")
            });
        return lista;
    }

    public List<TipoHospedaje> Tipos()
    {
        var lista = new List<TipoHospedaje>();
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("SELECT id, nombre FROM tipo_hospedaje ORDER BY nombre", c);
        using var r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(new TipoHospedaje { Id = r.GetInt32("id"), Nombre = r.GetString("nombre") });
        return lista;
    }

    // ---------- Validaciones de relaciones ----------
    // El destino debe existir y estar activo (o ser el que el hospedaje ya tenía).
    public bool DestinoPermitido(int destinoId, int destinoActualId)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM destino WHERE id = @d AND (activo = 1 OR id = @a)", c);
        cmd.Parameters.AddWithValue("@d", destinoId);
        cmd.Parameters.AddWithValue("@a", destinoActualId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public bool TipoExiste(int tipoId)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM tipo_hospedaje WHERE id = @t", c);
        cmd.Parameters.AddWithValue("@t", tipoId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public bool TieneReservas(int id)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM reserva WHERE hospedaje_id = @id", c);
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // ---------- ABM ----------
    public Hospedaje? Obtener(int id)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT id, destino_id, tipo_id, nombre, direccion, capacidad_maxima,
                                                  servicios, precio_noche, activo FROM hospedaje WHERE id = @id", c);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new Hospedaje
        {
            Id = r.GetInt32("id"), DestinoId = r.GetInt32("destino_id"), TipoId = r.GetInt32("tipo_id"),
            Nombre = r.GetString("nombre"), Direccion = Txt(r, "direccion"),
            CapacidadMaxima = r.GetInt32("capacidad_maxima"), Servicios = Txt(r, "servicios"),
            PrecioNoche = r.GetDecimal("precio_noche"), Activo = r.GetBoolean("activo")
        };
    }

    public int Alta(Hospedaje h)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"INSERT INTO hospedaje
            (destino_id, tipo_id, nombre, direccion, capacidad_maxima, servicios, precio_noche, activo)
            VALUES (@d, @t, @n, @dir, @cap, @serv, @p, 1)", c);
        Cargar(cmd, h);
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    public bool Modificar(Hospedaje h)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"UPDATE hospedaje SET destino_id = @d, tipo_id = @t, nombre = @n,
            direccion = @dir, capacidad_maxima = @cap, servicios = @serv, precio_noche = @p WHERE id = @id", c);
        Cargar(cmd, h);
        cmd.Parameters.AddWithValue("@id", h.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void Cargar(MySqlCommand cmd, Hospedaje h)
    {
        cmd.Parameters.AddWithValue("@d", h.DestinoId);
        cmd.Parameters.AddWithValue("@t", h.TipoId);
        cmd.Parameters.AddWithValue("@n", h.Nombre);
        cmd.Parameters.AddWithValue("@dir", (object?)h.Direccion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cap", h.CapacidadMaxima);
        cmd.Parameters.AddWithValue("@serv", (object?)h.Servicios ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", h.PrecioNoche);
    }

    public void CambiarActivo(int id, bool activo)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("UPDATE hospedaje SET activo = @a WHERE id = @id", c);
        cmd.Parameters.AddWithValue("@a", activo);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int id)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("DELETE FROM hospedaje WHERE id = @id", c); // las fotos caen por ON DELETE CASCADE
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ---------- Fotos ----------
    public List<FotoHospedaje> Fotos(int hospedajeId)
    {
        var lista = new List<FotoHospedaje>();
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT id, hospedaje_id, archivo, es_portada, subido_en
            FROM foto_hospedaje WHERE hospedaje_id = @h ORDER BY es_portada DESC, id", c);
        cmd.Parameters.AddWithValue("@h", hospedajeId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new FotoHospedaje
            {
                Id = r.GetInt32("id"), HospedajeId = r.GetInt32("hospedaje_id"), Archivo = r.GetString("archivo"),
                EsPortada = r.GetBoolean("es_portada"), SubidoEn = r.GetDateTime("subido_en")
            });
        return lista;
    }

    // La primera foto de un hospedaje queda como portada automáticamente.
    public void AltaFoto(int hospedajeId, string archivo)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var tx = c.BeginTransaction();
        int cantidad;
        using (var q = new MySqlCommand("SELECT COUNT(*) FROM foto_hospedaje WHERE hospedaje_id = @h", c, tx))
        {
            q.Parameters.AddWithValue("@h", hospedajeId);
            cantidad = Convert.ToInt32(q.ExecuteScalar());
        }
        using (var ins = new MySqlCommand(
            "INSERT INTO foto_hospedaje (hospedaje_id, archivo, es_portada) VALUES (@h, @a, @p)", c, tx))
        {
            ins.Parameters.AddWithValue("@h", hospedajeId);
            ins.Parameters.AddWithValue("@a", archivo);
            ins.Parameters.AddWithValue("@p", cantidad == 0);
            ins.ExecuteNonQuery();
        }
        tx.Commit();
    }

    // Deja una sola portada por hospedaje, en una transacción.
    public bool MarcarPortada(int fotoId)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var tx = c.BeginTransaction();
        int hospedajeId;
        using (var q = new MySqlCommand("SELECT hospedaje_id FROM foto_hospedaje WHERE id = @f", c, tx))
        {
            q.Parameters.AddWithValue("@f", fotoId);
            var o = q.ExecuteScalar();
            if (o == null) return false;
            hospedajeId = Convert.ToInt32(o);
        }
        using (var u = new MySqlCommand(
            "UPDATE foto_hospedaje SET es_portada = (id = @f) WHERE hospedaje_id = @h", c, tx))
        {
            u.Parameters.AddWithValue("@f", fotoId);
            u.Parameters.AddWithValue("@h", hospedajeId);
            u.ExecuteNonQuery();
        }
        tx.Commit();
        return true;
    }

    // Devuelve la ruta del archivo para que el controlador lo borre del disco.
    // Si la foto eliminada era la portada, otra pasa a serlo.
    public string? EliminarFoto(int fotoId)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var tx = c.BeginTransaction();
        string archivo;
        int hospedajeId;
        bool eraPortada;
        using (var q = new MySqlCommand("SELECT hospedaje_id, archivo, es_portada FROM foto_hospedaje WHERE id = @f", c, tx))
        {
            q.Parameters.AddWithValue("@f", fotoId);
            using var r = q.ExecuteReader();
            if (!r.Read()) return null;
            hospedajeId = r.GetInt32("hospedaje_id");
            archivo = r.GetString("archivo");
            eraPortada = r.GetBoolean("es_portada");
        }
        using (var d = new MySqlCommand("DELETE FROM foto_hospedaje WHERE id = @f", c, tx))
        {
            d.Parameters.AddWithValue("@f", fotoId);
            d.ExecuteNonQuery();
        }
        if (eraPortada)
        {
            using var u = new MySqlCommand(
                "UPDATE foto_hospedaje SET es_portada = 1 WHERE hospedaje_id = @h ORDER BY id LIMIT 1", c, tx);
            u.Parameters.AddWithValue("@h", hospedajeId);
            u.ExecuteNonQuery();
        }
        tx.Commit();
        return archivo;
    }
}
