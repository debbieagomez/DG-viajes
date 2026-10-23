namespace DG_viajes.Models;

public class Paquete
{
    public int Id { get; set; }

    public int DestinoId { get; set; }

    public string Nombre { get; set; }= string.Empty;

    public string? Descripcion { get; set; }

    public decimal PrecioPersona { get; set; }

    public DateTime FechaSalida { get; set; }

    public  DateTime FechaRegreso { get; set; }

    public int PlazasTotales { get; set; }

    public  bool IncluyeTransporte { get; set; }

    public string Estado { get; set; } = "Disponible";
}

//id, destino_id, nombre, descripcion, precio_persona, fecha_salida,
//  fecha_regreso, plazas_totales, incluye_transporte, estado