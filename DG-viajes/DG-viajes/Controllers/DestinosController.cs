using DG_viajes.Models;
using DG_viajes.Repositories;

namespace DG_viajes.Controllers;

public class DestinosController : ABMController<Destino>
{
    
    public DestinosController(IRepositorioDestino repositorio) : base(repositorio)
    {
        
    }

}