namespace DG_viajes.Models;

public class Hospedaje
{
    public int Id  { get; set; }

    public int DestinoId  { get; set; }

    public int TipoId  { get; set; }

    public string Nombre  { get; set; }= string.Empty;

    public string? Direccion { get; set; }

    public int CapacidadMaxima { get; set; }

    public string? Servicios { get; set; }

    public decimal PrecioNoche { get; set; }

    public bool Activo { get; set; }
}
//id, destino_id, tipo_id, nombre, direccion,capacidad_maxima
//servicios,precio_noche,activo 