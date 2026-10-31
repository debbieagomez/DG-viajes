using DG_viajes.Models;
using DG_viajes.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

public class ClientesController : ABMController<Cliente>
{
    private readonly IRepositorioCliente repositorioCliente;

    public ClientesController(IRepositorioCliente repositorio) : base(repositorio)
    {
        repositorioCliente = repositorio;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public override IActionResult Crear(Cliente entidad)
    {
        if ( repositorioCliente.ExisteDni(entidad.Dni))
        {
            ModelState.AddModelError("Dni", "Ya existe un cliente con ese DNI");
        }
        return base.Crear(entidad);
    }

     [HttpPost]
    [ValidateAntiForgeryToken]
    public override IActionResult Editar(Cliente entidad)
    {
        if (repositorioCliente.ExisteDni(entidad.Dni, entidad.Id))
        {
            ModelState.AddModelError("Dni", "Ya existe un un cliente con ese DNI");
        }
        return base.Editar(entidad);
    }

   

    
}