using DG_viajes.Models;
using DG_viajes.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

public class TiposHospedajeController : ABMController<TipoHospedaje>
{
    private readonly IRepositorioTipoHospedaje repo;

    public TiposHospedajeController(IRepositorioTipoHospedaje repositorio) : base(repositorio)
    {
        repo = repositorio;
    }

    // El nombre es UNIQUE: se controla antes de guardar para mostrar un mensaje claro.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public override IActionResult Crear(TipoHospedaje entidad)
    {
        ValidarNombre(entidad, 0);
        return base.Crear(entidad);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public override IActionResult Editar(TipoHospedaje entidad)
    {
        ValidarNombre(entidad, entidad.Id);
        return base.Editar(entidad);
    }

    private void ValidarNombre(TipoHospedaje entidad, int idExcluir)
    {
        entidad.Nombre = (entidad.Nombre ?? "").Trim();
        if (entidad.Nombre.Length == 0)
        {
            ModelState.Remove(nameof(TipoHospedaje.Nombre));
            ModelState.AddModelError(nameof(TipoHospedaje.Nombre), "El nombre es obligatorio.");
        }
        else if (entidad.Nombre.Length > 50)
        {
            ModelState.AddModelError(nameof(TipoHospedaje.Nombre), "El nombre no puede superar los 50 caracteres.");
        }
        else if (repo.ExisteNombre(entidad.Nombre, idExcluir))
        {
            ModelState.AddModelError(nameof(TipoHospedaje.Nombre), "Ese tipo ya existe");
        }
    }
}