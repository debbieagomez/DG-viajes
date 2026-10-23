namespace DG_viajes.Models;


public class Acompanante
{
    public int Id { get; set; }

    public int  ReservaId { get; set; }

    public string Dni { get; set; }= string.Empty;

    public string Nombre { get; set; }= string.Empty;

    public string Apellido { get; set; }= string.Empty;

    public string? Telefono { get; set; }
    
}
//id, reserva_id, dni, nombre, apellido,telefono