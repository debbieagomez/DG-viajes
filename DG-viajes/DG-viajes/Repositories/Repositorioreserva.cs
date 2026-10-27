using System.Data;
using DG_viajes.Models;
using DG_viajes.ViewModels;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace DG_viajes.Repositories;

public class RepositorioReserva
{
    public static readonly string[] Estados = { "Pendiente", "Confirmada", "Cancelada" };
    private readonly string cs;

    public RepositorioReserva(IConfiguration config)
    {
        cs = config.GetConnectionString("MySqlConnection")
             ?? throw new InvalidOperationException("Falta la cadena de conexión MySqlConnection.");
    }

    private static string? Txt(MySqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    private static object? Escalar(MySqlConnection c, MySqlTransaction tx, string sql, params (string, object)[] ps)
    {
        using var cmd = new MySqlCommand(sql, c, tx);
        foreach (var (nombre, valor) in ps) cmd.Parameters.AddWithValue(nombre, valor);
        return cmd.ExecuteScalar();
    }

    // ======================= Búsquedas AJAX del formulario =======================
    public List<ClienteOpcion> BuscarClientes(string? q)
    {
        q = (q ?? "").Trim();
        var lista = new List<ClienteOpcion>();
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT id, dni, nombre, apellido FROM cliente
            WHERE activo = 1 AND (@q = '' OR dni LIKE @like OR nombre LIKE @like OR apellido LIKE @like
                                  OR CONCAT(apellido, ' ', nombre) LIKE @like OR CONCAT(nombre, ' ', apellido) LIKE @like)
            ORDER BY apellido, nombre LIMIT 10", c);
        cmd.Parameters.AddWithValue("@q", q);
        cmd.Parameters.AddWithValue("@like", "%" + q + "%");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new ClienteOpcion(r.GetInt32("id"), r.GetString("dni"), r.GetString("nombre"), r.GetString("apellido")));
        return lista;
    }

    // Solo paquetes disponibles, de destino activo, que todavía no salieron y con plazas libres.
    public List<PaqueteOpcion> BuscarPaquetes(string? q)
    {
        q = (q ?? "").Trim();
        var lista = new List<PaqueteOpcion>();
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT p.id, p.nombre, p.destino_id, d.ciudad, p.precio_persona,
                p.fecha_salida, p.fecha_regreso, DATEDIFF(p.fecha_regreso, p.fecha_salida) AS noches,
                p.plazas_totales - COALESCE((SELECT SUM(r.cant_pasajeros) FROM reserva r
                    WHERE r.paquete_id = p.id AND r.estado <> 'Cancelada'), 0) AS libres
            FROM paquete p JOIN destino d ON d.id = p.destino_id
            WHERE p.estado = 'Disponible' AND d.activo = 1 AND p.fecha_salida >= @hoy
              AND (@q = '' OR p.nombre LIKE @like OR d.ciudad LIKE @like)
            HAVING libres > 0
            ORDER BY p.fecha_salida LIMIT 10", c);
        cmd.Parameters.AddWithValue("@hoy", DateTime.Today);
        cmd.Parameters.AddWithValue("@q", q);
        cmd.Parameters.AddWithValue("@like", "%" + q + "%");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new PaqueteOpcion(
                r.GetInt32("id"), r.GetString("nombre"), r.GetInt32("destino_id"), r.GetString("ciudad"),
                r.GetDecimal("precio_persona"), r.GetDateTime("fecha_salida"), r.GetDateTime("fecha_regreso"),
                r.GetInt32("noches"), Convert.ToInt32(r.GetValue(r.GetOrdinal("libres")))));
        return lista;
    }

    // Hospedajes activos del destino del paquete, con lugar libre en las fechas del viaje.
    public List<HospedajeOpcion> BuscarHospedajes(int paqueteId, string? q)
    {
        q = (q ?? "").Trim();
        var lista = new List<HospedajeOpcion>();
        using var c = new MySqlConnection(cs);
        c.Open();
        using var cmd = new MySqlCommand(@"SELECT h.id, h.nombre, t.nombre AS tipo, h.direccion, h.capacidad_maxima, h.precio_noche,
                h.capacidad_maxima - COALESCE((SELECT SUM(r.cant_pasajeros) FROM reserva r
                    JOIN paquete p2 ON p2.id = r.paquete_id
                    WHERE r.hospedaje_id = h.id AND r.estado <> 'Cancelada'
                      AND p2.fecha_salida < pq.fecha_regreso AND p2.fecha_regreso > pq.fecha_salida), 0) AS libres
            FROM paquete pq
            JOIN hospedaje h ON h.destino_id = pq.destino_id
            JOIN tipo_hospedaje t ON t.id = h.tipo_id
            WHERE pq.id = @paq AND h.activo = 1 AND (@q = '' OR h.nombre LIKE @like OR t.nombre LIKE @like)
            HAVING libres > 0
            ORDER BY h.nombre LIMIT 10", c);
        cmd.Parameters.AddWithValue("@paq", paqueteId);
        cmd.Parameters.AddWithValue("@q", q);
        cmd.Parameters.AddWithValue("@like", "%" + q + "%");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(new HospedajeOpcion(
                r.GetInt32("id"), r.GetString("nombre"), r.GetString("tipo"), Txt(r, "direccion"),
                r.GetInt32("capacidad_maxima"), r.GetDecimal("precio_noche"),
                Convert.ToInt32(r.GetValue(r.GetOrdinal("libres")))));
        return lista;
    }

    // ======================= Alta de reserva =======================
    // Todo ocurre en una transacción. Se bloquea primero la fila del paquete y después la del hospedaje
    // (siempre en ese orden), así dos empleados no pueden vender la misma plaza a la vez.
    public ResultadoReserva Crear(ReservaNueva n, int usuarioId)
    {
        var errores = new List<string>();
        using var c = new MySqlConnection(cs);
        c.Open();
        using var tx = c.BeginTransaction(IsolationLevel.ReadCommitted);

        // Cliente
        var dniTitular = Escalar(c, tx, "SELECT dni FROM cliente WHERE id = @id AND activo = 1", ("@id", n.ClienteId)) as string;
        if (dniTitular == null) errores.Add("Elegí un cliente activo.");

        // Paquete
        int destinoPaquete = 0, plazasTotales = 0;
        decimal precioPersona = 0;
        DateTime salida = default, regreso = default;
        bool paqueteOk = false;
        using (var cmd = new MySqlCommand(@"SELECT destino_id, precio_persona, fecha_salida, fecha_regreso, plazas_totales, estado
                                            FROM paquete WHERE id = @id FOR UPDATE", c, tx))
        {
            cmd.Parameters.AddWithValue("@id", n.PaqueteId);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                paqueteOk = true;
                destinoPaquete = r.GetInt32("destino_id");
                precioPersona = r.GetDecimal("precio_persona");
                salida = r.GetDateTime("fecha_salida");
                regreso = r.GetDateTime("fecha_regreso");
                plazasTotales = r.GetInt32("plazas_totales");
                if (r.GetString("estado") != "Disponible") errores.Add("El paquete está suspendido.");
            }
            else errores.Add("Elegí un paquete.");
        }
        if (paqueteOk)
        {
            if (salida.Date < DateTime.Today) errores.Add("El paquete ya salió: no admite reservas nuevas.");
            var destinoActivo = Escalar(c, tx, "SELECT activo FROM destino WHERE id = @id", ("@id", destinoPaquete));
            if (destinoActivo == null || !Convert.ToBoolean(destinoActivo)) errores.Add("El destino del paquete está desactivado.");
        }

        // Hospedaje
        int capacidad = 0;
        decimal precioNoche = 0;
        bool hospedajeOk = false;
        using (var cmd = new MySqlCommand(@"SELECT destino_id, capacidad_maxima, precio_noche, activo
                                            FROM hospedaje WHERE id = @id FOR UPDATE", c, tx))
        {
            cmd.Parameters.AddWithValue("@id", n.HospedajeId);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                hospedajeOk = true;
                capacidad = r.GetInt32("capacidad_maxima");
                precioNoche = r.GetDecimal("precio_noche");
                if (!r.GetBoolean("activo")) errores.Add("El hospedaje está desactivado.");
                if (paqueteOk && r.GetInt32("destino_id") != destinoPaquete)
                    errores.Add("El hospedaje debe ser del mismo destino que el paquete.");
            }
            else errores.Add("Elegí un hospedaje.");
        }

        if (dniTitular != null && n.Acompanantes.Any(a => a.Dni == dniTitular))
            errores.Add("Un acompañante no puede tener el mismo DNI que el titular.");

        if (errores.Count > 0 || !paqueteOk || !hospedajeOk) return new ResultadoReserva(errores, 0);

        // Plazas del paquete (las reservas canceladas no cuentan)
        int ocupadasPaquete = Convert.ToInt32(Escalar(c, tx,
            "SELECT COALESCE(SUM(cant_pasajeros), 0) FROM reserva WHERE paquete_id = @p AND estado <> 'Cancelada'",
            ("@p", n.PaqueteId)));
        int libresPaquete = plazasTotales - ocupadasPaquete;
        if (n.CantPasajeros > libresPaquete)
            errores.Add($"El paquete solo tiene {libresPaquete} plazas disponibles.");

        // Capacidad del hospedaje en las fechas del viaje (reservas de otros paquetes que se solapan)
        int ocupadasHospedaje = Convert.ToInt32(Escalar(c, tx,
            @"SELECT COALESCE(SUM(r.cant_pasajeros), 0) FROM reserva r JOIN paquete p ON p.id = r.paquete_id
              WHERE r.hospedaje_id = @h AND r.estado <> 'Cancelada' AND p.fecha_salida < @reg AND p.fecha_regreso > @sal",
            ("@h", n.HospedajeId), ("@reg", regreso.Date), ("@sal", salida.Date)));
        int libresHospedaje = capacidad - ocupadasHospedaje;
        if (n.CantPasajeros > libresHospedaje)
            errores.Add($"El hospedaje no tiene capacidad para esas fechas (lugares libres: {Math.Max(libresHospedaje, 0)}).");

        if (errores.Count > 0) return new ResultadoReserva(errores, 0);

        // Importe congelado: precio por persona x pasajeros + precio por noche x noches
        int noches = (regreso.Date - salida.Date).Days;
        decimal total = precioPersona * n.CantPasajeros + precioNoche * noches;

        int reservaId;
        using (var ins = new MySqlCommand(@"INSERT INTO reserva
            (cliente_id, paquete_id, hospedaje_id, fecha_reserva, cant_pasajeros, precio_total, estado, creado_por)
            VALUES (@c, @p, @h, @f, @cant, @total, 'Pendiente', @u)", c, tx))
        {
            ins.Parameters.AddWithValue("@c", n.ClienteId);
            ins.Parameters.AddWithValue("@p", n.PaqueteId);
            ins.Parameters.AddWithValue("@h", n.HospedajeId);
            ins.Parameters.AddWithValue("@f", DateTime.Today);
            ins.Parameters.AddWithValue("@cant", n.CantPasajeros);
            ins.Parameters.AddWithValue("@total", total);
            ins.Parameters.AddWithValue("@u", usuarioId);
            ins.ExecuteNonQuery();
            reservaId = (int)ins.LastInsertedId;
        }
        foreach (var a in n.Acompanantes)
        {
            using var ia = new MySqlCommand(@"INSERT INTO acompanante (reserva_id, dni, nombre, apellido, telefono)
                VALUES (@r, @dni, @n, @ap, @t)", c, tx);
            ia.Parameters.AddWithValue("@r", reservaId);
            ia.Parameters.AddWithValue("@dni", a.Dni);
            ia.Parameters.AddWithValue("@n", a.Nombre);
            ia.Parameters.AddWithValue("@ap", a.Apellido);
            ia.Parameters.AddWithValue("@t", (object?)a.Telefono ?? DBNull.Value);
            ia.ExecuteNonQuery();
        }
        tx.Commit();
        return new ResultadoReserva(errores, reservaId);
    }

    // ======================= Confirmar / cancelar =======================
    // Devuelve null si salió bien, o el mensaje de error.
    public string? CambiarEstado(int id, string nuevo, int usuarioId)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        using var tx = c.BeginTransaction(IsolationLevel.ReadCommitted);
        string? actual;
        using (var q = new MySqlCommand("SELECT estado FROM reserva WHERE id = @id FOR UPDATE", c, tx))
        {
            q.Parameters.AddWithValue("@id", id);
            actual = q.ExecuteScalar() as string;
        }
        if (actual == null) return "La reserva no existe.";
        if (nuevo == "Confirmada" && actual != "Pendiente") return "Solo se puede confirmar una reserva pendiente.";
        if (nuevo == "Cancelada" && actual == "Cancelada") return "La reserva ya está cancelada.";
        if (nuevo != "Confirmada" && nuevo != "Cancelada") return "Estado no válido.";

        using (var u = new MySqlCommand(
            "UPDATE reserva SET estado = @e, modificado_por = @u, modificado_en = @f WHERE id = @id", c, tx))
        {
            u.Parameters.AddWithValue("@e", nuevo);
            u.Parameters.AddWithValue("@u", usuarioId);
            u.Parameters.AddWithValue("@f", DateTime.Now);
            u.Parameters.AddWithValue("@id", id);
            u.ExecuteNonQuery();
        }
        tx.Commit();
        return null;
    }

    // ======================= Listado y detalle =======================
    public Paginado<ReservaFila> Listar(ReservaFiltro f)
    {
        int tam = Math.Clamp(f.Tamanio, 1, 50);
        int pag = Math.Max(f.Pagina, 1);
        var cond = new List<string>();
        bool buscar = !string.IsNullOrWhiteSpace(f.Buscar);
        bool estadoValido = f.Estado != null && Estados.Contains(f.Estado);
        if (buscar)
            cond.Add(@"(c.dni LIKE @q OR c.nombre LIKE @q OR c.apellido LIKE @q
                        OR CONCAT(c.apellido, ' ', c.nombre) LIKE @q OR CONCAT(c.nombre, ' ', c.apellido) LIKE @q)");
        if (estadoValido) cond.Add("r.estado = @estado");
        string where = cond.Count > 0 ? "WHERE " + string.Join(" AND ", cond) : "";
        const string from = @"FROM reserva r JOIN cliente c ON c.id = r.cliente_id JOIN paquete p ON p.id = r.paquete_id
                              JOIN destino d ON d.id = p.destino_id JOIN hospedaje h ON h.id = r.hospedaje_id";

        void Params(MySqlCommand cmd)
        {
            if (buscar) cmd.Parameters.AddWithValue("@q", "%" + f.Buscar!.Trim() + "%");
            if (estadoValido) cmd.Parameters.AddWithValue("@estado", f.Estado);
        }

        using var c2 = new MySqlConnection(cs);
        c2.Open();
        int total;
        using (var cc = new MySqlCommand($"SELECT COUNT(*) {from} {where}", c2))
        {
            Params(cc);
            total = Convert.ToInt32(cc.ExecuteScalar());
        }

        var items = new List<ReservaFila>();
        using (var cmd = new MySqlCommand($@"SELECT r.id, CONCAT(c.apellido, ', ', c.nombre) AS cliente, c.dni,
                p.nombre AS paquete, d.ciudad AS destino, h.nombre AS hospedaje, r.fecha_reserva, r.cant_pasajeros,
                r.precio_total, r.estado,
                COALESCE((SELECT SUM(pg.importe) FROM pago pg WHERE pg.reserva_id = r.id AND pg.anulado_en IS NULL), 0) AS pagado
            {from} {where} ORDER BY r.id DESC LIMIT @tam OFFSET @off", c2))
        {
            Params(cmd);
            cmd.Parameters.AddWithValue("@tam", tam);
            cmd.Parameters.AddWithValue("@off", (pag - 1) * tam);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                items.Add(new ReservaFila(
                    r.GetInt32("id"), r.GetString("cliente"), r.GetString("dni"), r.GetString("paquete"),
                    r.GetString("destino"), r.GetString("hospedaje"), r.GetDateTime("fecha_reserva"),
                    r.GetInt32("cant_pasajeros"), r.GetDecimal("precio_total"), r.GetDecimal("pagado"), r.GetString("estado")));
        }
        return new Paginado<ReservaFila>(items, total, pag, tam);
    }

    public ReservaDetalle? Detalle(int id)
    {
        using var c = new MySqlConnection(cs);
        c.Open();
        ReservaDetalle? d = null;
        using (var cmd = new MySqlCommand(@"SELECT r.id, r.cliente_id, r.paquete_id, r.hospedaje_id, r.fecha_reserva,
                r.cant_pasajeros, r.precio_total, r.estado, r.creado_por, r.creado_en, r.modificado_por, r.modificado_en,
                c.dni AS cdni, c.nombre AS cnom, c.apellido AS cape, c.email AS cemail, c.telefono AS ctel,
                p.nombre AS pnom, dd.ciudad AS dciudad, p.fecha_salida, p.fecha_regreso,
                h.nombre AS hnom, t.nombre AS tnom,
                CONCAT(uc.nombre, ' ', uc.apellido) AS creador, CONCAT(um.nombre, ' ', um.apellido) AS modificador,
                COALESCE((SELECT SUM(pg.importe) FROM pago pg WHERE pg.reserva_id = r.id AND pg.anulado_en IS NULL), 0) AS pagado
            FROM reserva r JOIN cliente c ON c.id = r.cliente_id JOIN paquete p ON p.id = r.paquete_id
            JOIN destino dd ON dd.id = p.destino_id JOIN hospedaje h ON h.id = r.hospedaje_id
            JOIN tipo_hospedaje t ON t.id = h.tipo_id JOIN usuario uc ON uc.id = r.creado_por
            LEFT JOIN usuario um ON um.id = r.modificado_por WHERE r.id = @id", c))
        {
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            int im = r.GetOrdinal("modificado_por"), ie = r.GetOrdinal("modificado_en");
            d = new ReservaDetalle
            {
                Reserva = new Reserva
                {
                    Id = r.GetInt32("id"), ClienteId = r.GetInt32("cliente_id"), PaqueteId = r.GetInt32("paquete_id"),
                    HospedajeId = r.GetInt32("hospedaje_id"), FechaReserva = r.GetDateTime("fecha_reserva"),
                    CantPasajeros = r.GetInt32("cant_pasajeros"), PrecioTotal = r.GetDecimal("precio_total"),
                    Estado = r.GetString("estado"), CreadoPor = r.GetInt32("creado_por"), CreadoEn = r.GetDateTime("creado_en"),
                    ModificadoPor = r.IsDBNull(im) ? null : r.GetInt32(im),
                    ModificadoEn = r.IsDBNull(ie) ? null : r.GetDateTime(ie)
                },
                ClienteNombre = r.GetString("cape") + ", " + r.GetString("cnom"),
                ClienteDni = r.GetString("cdni"), ClienteEmail = Txt(r, "cemail"), ClienteTelefono = Txt(r, "ctel"),
                Paquete = r.GetString("pnom"), Destino = r.GetString("dciudad"),
                FechaSalida = r.GetDateTime("fecha_salida"), FechaRegreso = r.GetDateTime("fecha_regreso"),
                Hospedaje = r.GetString("hnom"), TipoHospedaje = r.GetString("tnom"),
                Pagado = r.GetDecimal("pagado"), CreadoPor = r.GetString("creador"), ModificadoPor = Txt(r, "modificador")
            };
        }
        using (var cmd = new MySqlCommand(
            "SELECT id, reserva_id, dni, nombre, apellido, telefono FROM acompanante WHERE reserva_id = @id ORDER BY id", c))
        {
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                d.Acompanantes.Add(new Acompanante
                {
                    Id = r.GetInt32("id"), ReservaId = r.GetInt32("reserva_id"), Dni = r.GetString("dni"),
                    Nombre = r.GetString("nombre"), Apellido = r.GetString("apellido"), Telefono = Txt(r, "telefono")
                });
        }
        return d;
    }
}