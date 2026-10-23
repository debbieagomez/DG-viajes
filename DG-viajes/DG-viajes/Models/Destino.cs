namespace DG_viajes.Models;

public class Destino
{
    public int Id { get; set; }

    public string Pais { get; set; }= string.Empty;

    public string? Provincia { get; set; }

    public string Ciudad { get; set; }= string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

}