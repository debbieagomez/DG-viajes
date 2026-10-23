namespace DG_viajes.Models;

public class FotoHospedaje
{
    public int Id { get; set; }

    public int HospedajeId { get; set; }

    public string Archivo { get; set; }= string.Empty;

    public bool EsPortada { get; set; }

    public DateTime SubidoEn { get; set; }
}
