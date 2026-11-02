using System.Data;
using System.Globalization;
using DG_viajes.Models;
using DG_viajes.ViewModels;
using MySqlConnector;

namespace DG_viajes.Repositories;

public class RepositorioPago : IRepositorioPago
{
    private static readonly CultureInfo Es = new("es-AR");
    private readonly string connectionString;

    public RepositorioPago(string connectionString)
    {
        this.connectionString = connectionString;
    }

    private static string? Txt(MySqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    public ResumenReservaPagos? ObtenerResumen(int reservaId)
    {
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT r.id, CONCAT(c.apellido, ', ', c.nombre) AS cliente, c.dni,
                p.nombre AS paquete, r.estado, r.precio_total,
                COALESCE((SELECT SUM(pg.importe) FROM pago pg WHERE pg.reserva_id = r.id AND pg.anulado_en IS NULL), 0) AS pagado
            FROM reserva r JOIN cliente c ON c.id = r.cliente_id JOIN paquete p ON p.id = r.paquete_id
            WHERE r.id = @id", c);
        cmd.Parameters.AddWithValue("@id", reservaId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new ResumenReservaPagos
        {
            ReservaId = r.GetInt32("id"), Cliente = r.GetString("cliente"), Dni = r.GetString("dni"),
            Paquete = r.GetString("paquete"), Estado = r.GetString("estado"),
            PrecioTotal = r.GetDecimal("precio_total"), Pagado = r.GetDecimal("pagado")
        };
    }

    public IList<PagoFila> ObtenerPagos(int reservaId)
    {
        var lista = new List<PagoFila>();
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT pg.id, pg.tipo, pg.fecha, pg.importe, pg.medio_pago, pg.creado_en, pg.anulado_en,
                CONCAT(uc.nombre, ' ', uc.apellido) AS creador, CONCAT(ua.nombre, ' ', ua.apellido) AS anulador
            FROM pago pg JOIN usuario uc ON uc.id = pg.creado_por LEFT JOIN usuario ua ON ua.id = pg.anulado_por
            WHERE pg.reserva_id = @id ORDER BY pg.fecha DESC, pg.id DESC", c);
        cmd.Parameters.AddWithValue("@id", reservaId);
        using var r = cmd.ExecuteReader();
        int ia = r.GetOrdinal("anulado_en");
        while (r.Read())
            lista.Add(new PagoFila(
                r.GetInt32("id"), r.GetString("tipo"), r.GetDateTime("fecha"), r.GetDecimal("importe"),
                r.GetString("medio_pago"), r.GetString("creador"), r.GetDateTime("creado_en"),
                Txt(r, "anulador"), r.IsDBNull(ia) ? null : r.GetDateTime(ia)));
        return lista;
    }

    // Se bloquea la fila de la reserva: dos pagos simultáneos no pueden superar el saldo entre los dos.
    public List<string> Registrar(Pago p)
    {
        var errores = new List<string>();
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var tx = c.BeginTransaction(IsolationLevel.ReadCommitted);

        string? estado = null;
        decimal total = 0;
        using (var q = new MySqlCommand("SELECT estado, precio_total FROM reserva WHERE id = @id FOR UPDATE", c, tx))
        {
            q.Parameters.AddWithValue("@id", p.ReservaId);
            using var r = q.ExecuteReader();
            if (r.Read())
            {
                estado = r.GetString("estado");
                total = r.GetDecimal("precio_total");
            }
        }
        if (estado == null) { errores.Add("La reserva no existe."); return errores; }
        if (estado == "Cancelada") { errores.Add("Una reserva cancelada no puede recibir pagos."); return errores; }
        if (p.Importe <= 0) { errores.Add("El importe debe ser mayor a cero."); return errores; }

        decimal pagado;
        using (var q = new MySqlCommand(
            "SELECT COALESCE(SUM(importe), 0) FROM pago WHERE reserva_id = @id AND anulado_en IS NULL", c, tx))
        {
            q.Parameters.AddWithValue("@id", p.ReservaId);
            pagado = Convert.ToDecimal(q.ExecuteScalar());
        }

        decimal saldo = total - pagado;
        if (saldo <= 0) { errores.Add("La reserva no tiene saldo pendiente."); return errores; }
        if (p.Tipo == "Total")
        {
            // Un pago Total cancela la deuda: tiene que ser igual al saldo que falta.
            if (p.Importe != saldo)
            {
                errores.Add($"Un pago Total debe ser igual al saldo pendiente ({saldo.ToString("C0", Es)}).");
                return errores;
            }
        }
        else if (p.Importe >= saldo)
        {
            // Seña y Parcial dejan saldo: tienen que ser menores a lo que falta.
            errores.Add($"Una seña o un pago parcial debe ser menor al saldo pendiente ({saldo.ToString("C0", Es)}). Si cancela la deuda, registralo como Total.");
            return errores;
        }

        using (var ins = new MySqlCommand(@"INSERT INTO pago (reserva_id, tipo, fecha, importe, medio_pago, creado_por)
            VALUES (@r, @t, @f, @i, @m, @u)", c, tx))
        {
            ins.Parameters.AddWithValue("@r", p.ReservaId);
            ins.Parameters.AddWithValue("@t", p.Tipo);
            ins.Parameters.AddWithValue("@f", p.Fecha.Date);
            ins.Parameters.AddWithValue("@i", p.Importe);
            ins.Parameters.AddWithValue("@m", p.MedioPago);
            ins.Parameters.AddWithValue("@u", p.CreadoPor);
            ins.ExecuteNonQuery();
        }
        tx.Commit();
        return errores;
    }

    // Un pago no se borra: se anula guardando quién y cuándo.
    public string? Anular(int pagoId, int usuarioId)
    {
        using var c = new MySqlConnection(connectionString);
        c.Open();
        using var tx = c.BeginTransaction(IsolationLevel.ReadCommitted);

        bool existe = false, yaAnulado = false;
        using (var q = new MySqlCommand("SELECT anulado_en FROM pago WHERE id = @id FOR UPDATE", c, tx))
        {
            q.Parameters.AddWithValue("@id", pagoId);
            using var r = q.ExecuteReader();
            if (r.Read())
            {
                existe = true;
                yaAnulado = !r.IsDBNull(0);
            }
        }
        if (!existe) return "El pago no existe.";
        if (yaAnulado) return "El pago ya está anulado.";

        using (var u = new MySqlCommand("UPDATE pago SET anulado_por = @u, anulado_en = @f WHERE id = @id", c, tx))
        {
            u.Parameters.AddWithValue("@u", usuarioId);
            u.Parameters.AddWithValue("@f", DateTime.Now);
            u.Parameters.AddWithValue("@id", pagoId);
            u.ExecuteNonQuery();
        }
        tx.Commit();
        return null;
    }
}