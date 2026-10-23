namespace DG_viajes.Models;

public class Usuario
{
   public int Id { get; set; }

   public string Email { get; set; }= string.Empty;

   public  string PasswordHash { get; set; }= string.Empty;

   public string Nombre { get; set; }= string.Empty;

   public string Apellido { get; set; }= string.Empty;

   public string? Avatar { get; set; }

   public string Rol { get; set; }= string.Empty;

   public bool Activo { get; set; }

}
