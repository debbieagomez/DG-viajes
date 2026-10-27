using DG_viajes.Models;
using DG_viajes.ViewModels;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace DG_viajes.Repositories;

public class RepositorioPaquete
{
    public static readonly string[] Estados = { "Disponible", "Suspendido" };
    private readonly string cs;

    public RepositorioPaquete(IConfiguration config)
    {
        cs = config.GetConnectionString("MySqlConnection")
             ?? throw new InvalidOperationException("Falta la cadena de conexión MySqlConnection.");
    }

    private static string? Txt(MySqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    // ---------- Listado: filtros combinables + paginado por servidor ----------
    public Paginado<PaqueteFila> Listar(PaqueteFiltro f)
    {
        int tam = Math.Clamp(f.Tamanio, 1, 50);
        int pag = Math.Max(f.Pagina, 1);

        var cond = new List<string>();
        if (f.DestinoId.HasValue) cond.Add("p.destino_id = @dest");
        if (f.PrecioMin.HasValue) cond.Add("p.precio_persona >= @pmin");
        if (f.PrecioMax.HasValue) cond.Add("p.precio_persona <= @pmax");
        if (f.SalidaDesde.HasValue) cond.Add("p.fecha_salida >= @desde");
        if (f.SalidaHasta.HasValue) cond.Add("p.fecha_salida <= @hasta");
        bool estadoValido = f.Estado != null && Estados.Contains(f.Estado);
        if (estadoValido) cond.Add("p.estado = @estado");

        string where = cond.Count > 0 ? "WHERE " + string.Join(" AND ", cond) : "";
        const string from = "FROM paquete p JOIN destino d ON d.id = p.destino_id";

        void Params(MySqlCommand cmd)
        {
            if (f.DestinoId.HasValue) cmd.Parameters.AddWithValue("@dest", f.DestinoId.Value);
            if (f.PrecioMin.HasValue) cmd.Parameters.AddWithValue("@pmin", f.PrecioMin.Value);
            if (f.PrecioMax.HasValue) cmd.Parameters.AddWithValue("@pmax", f.PrecioMax.Value);
            if (f.SalidaDesde.HasValue) cmd.Parameters.AddWithValue("@desde", f.SalidaDesde.Value.Date);
            if (f.SalidaHasta.HasValue) cmd.Parameters.AddWithValue("@hasta", f.SalidaHasta.Value.Date);
            if (estadoValido) cmd.Parameters.AddWithValue("@estado", f.Estado);
        }

        using var c = new MySqlConnection(cs);
        c.Open();

        int total;
        using (var cc = new MySqlCommand($"SELECT COUNT(*) {from} {where}", c))
        {
            Params(cc);
            total = Convert.ToInt32(cc.ExecuteScalar());
        }

        var items = new List<PaqueteFila>();
        string sql = $@"SELECT p.id, p.destino_id, d.ciudad, p.nombre, p.precio_persona, p.fecha_salida,
                               p.fecha_regreso, p.plazas_totales, p.incluye_transporte, p.estado,
                               COALESCE((SELECT SUM(r.cant_pasajeros) FROM reserva r
                                         WHERE r.paquete_id = p.id AND r.estado <> 'Cancelada'), 0) AS ocupadas
                        {from} {where}
                        ORDER BY p.fecha_salida, p.id LIMIT @tam OFFSET @off";
        using (var cmd = new MySqlCommand(sql, c))
        {
            Params(cmd);
            cmd.Parameters.AddWithValue("@tam", tam);
            cmd.Parameters.AddWithValue("@off", (pag - 1) * tam);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                items.Add(new PaqueteFila(
                    r.GetInt32("id"), r.GetInt32("destino_id"), r.GetString("ciudad"), r.GetString("nombre"),
                    r.GetDecimal("precio_persona"), r.GetDateTime("fecha_salida"), r.GetDateTime("fecha_regreso"),
                    r.GetInt32("plazas_totales"), Convert.ToInt32(r.GetValue(r.GetOrdinal("ocupadas"))),
                    r.GetBoolean("incluye_transporte"), r.GetString("estado")));
            }
        }
        return new Paginado<PaqueteFila>(items, total, pag, tam);
    }

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

    // ---------- Reglas de integridad ----------
    public bool DestinoPermitido(int destinoId, int destinoActualId)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM destino WHERE id = @d AND (activo = 1 OR id = @a)", c);
        cmd.Parameters.AddWithValue("@d", destinoId);
        cmd.Parameters.AddWithValue("@a", destinoActualId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public bool TieneReservas(int id)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM reserva WHERE paquete_id = @id", c);
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // Pasajeros en reservas no canceladas.
    public int PlazasOcupadas(int id)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(
            "SELECT COALESCE(SUM(cant_pasajeros), 0) FROM reserva WHERE paquete_id = @id AND estado <> 'Cancelada'", c);
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // ---------- ABM ----------
    public Paquete? Obtener(int id)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT id, destino_id, nombre, descripcion, precio_persona, fecha_salida,
                fecha_regreso, plazas_totales, incluye_transporte, estado FROM paquete WHERE id = @id", c);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new Paquete
        {
            Id = r.GetInt32("id"), DestinoId = r.GetInt32("destino_id"), Nombre = r.GetString("nombre"),
            Descripcion = Txt(r, "descripcion"), PrecioPersona = r.GetDecimal("precio_persona"),
            FechaSalida = r.GetDateTime("fecha_salida"), FechaRegreso = r.GetDateTime("fecha_regreso"),
            PlazasTotales = r.GetInt32("plazas_totales"), IncluyeTransporte = r.GetBoolean("incluye_transporte"),
            Estado = r.GetString("estado")
        };
    }

    public int Alta(Paquete p)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"INSERT INTO paquete
            (destino_id, nombre, descripcion, precio_persona, fecha_salida, fecha_regreso, plazas_totales, incluye_transporte, estado)
            VALUES (@d, @n, @desc, @precio, @sal, @reg, @plazas, @trans, 'Disponible')", c);
        Cargar(cmd, p);
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    public bool Modificar(Paquete p)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"UPDATE paquete SET destino_id = @d, nombre = @n, descripcion = @desc,
            precio_persona = @precio, fecha_salida = @sal, fecha_regreso = @reg, plazas_totales = @plazas,
            incluye_transporte = @trans WHERE id = @id", c);
        Cargar(cmd, p);
        cmd.Parameters.AddWithValue("@id", p.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void Cargar(MySqlCommand cmd, Paquete p)
    {
        cmd.Parameters.AddWithValue("@d", p.DestinoId);
        cmd.Parameters.AddWithValue("@n", p.Nombre);
        cmd.Parameters.AddWithValue("@desc", (object?)p.Descripcion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@precio", p.PrecioPersona);
        cmd.Parameters.AddWithValue("@sal", p.FechaSalida.Date);
        cmd.Parameters.AddWithValue("@reg", p.FechaRegreso.Date);
        cmd.Parameters.AddWithValue("@plazas", p.PlazasTotales);
        cmd.Parameters.AddWithValue("@trans", p.IncluyeTransporte);
    }

    public void CambiarEstado(int id, string estado)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("UPDATE paquete SET estado = @e WHERE id = @id", c);
        cmd.Parameters.AddWithValue("@e", estado);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int id)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand("DELETE FROM paquete WHERE id = @id", c);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
