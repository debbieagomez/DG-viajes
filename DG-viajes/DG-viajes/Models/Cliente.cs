namespace DG_viajes.Models;

public class Cliente
{
    public int Id { get; set; }

    public  string Dni { get; set; }= string.Empty;

    public string Nombre { get; set; }= string.Empty;

    public string Apellido { get; set; }= string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string?  Domicilio { get; set; }

    public bool Activo { get; set; }
}

