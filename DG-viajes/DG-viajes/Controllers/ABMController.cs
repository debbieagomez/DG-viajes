using DG_viajes.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DG_viajes.Controllers;

 [Authorize]   
public abstract class ABMController<T> : Controller where T : new()
{
    protected readonly IRepositorio<T> repositorio;

    protected ABMController(IRepositorio<T> repositorio)
    {
        this.repositorio = repositorio;
    }

    private string ClaveError =>
        $"Error_{ControllerContext.ActionDescriptor.ControllerName}";

    protected void MostrarError(string mensaje)
    {
        TempData[ClaveError] = mensaje;
    }

    // LISTADO con búsqueda y paginado por servidor
    public virtual IActionResult Index(string? busqueda, int pagina = 1)
    {
        const int tamPagina = 10;

        if (pagina < 1)
        {
            pagina = 1;
        }

        var cantidad = repositorio.ObtenerCantidad(busqueda);

        var totalPaginas = cantidad == 0
            ? 1
            : (int)Math.Ceiling((double)cantidad / tamPagina);

        if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        var lista = repositorio.ObtenerLista(busqueda, pagina, tamPagina);

        ViewBag.Busqueda = busqueda;
        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.Cantidad = cantidad;

        return View(lista);
    }

    // CREAR: muestra el formulario vacío
    [HttpGet]
    public virtual IActionResult Crear()
    {
        return View(new T());
    }

    // CREAR: recibe el formulario completado y guarda
    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual IActionResult Crear(T entidad)
    {
        if (!ModelState.IsValid)
        {
            return View(entidad);
        }

        try
        {
            repositorio.Alta(entidad);
        }
        catch (MySqlException)
        {
            MostrarError("No se pudo guardar el registro. Verifique los datos e inténtelo nuevamente.");
        }

        return RedirectToAction(nameof(Index));
    }

    // EDITAR: muestra el formulario con los datos actuales
    [HttpGet]
    public virtual IActionResult Editar(int id)
    {
        var entidad = repositorio.ObtenerPorId(id);

        if (entidad == null)
        {
            return NotFound();
        }

        return View(entidad);
    }

    // EDITAR: recibe el formulario modificado y guarda
    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual IActionResult Editar(T entidad)
    {
        if (!ModelState.IsValid)
        {
            return View(entidad);
        }

        try
        {
            repositorio.Modificacion(entidad);
        }
        catch (MySqlException)
        {
            MostrarError("No se pudo modificar el registro. Verifique los datos e inténtelo nuevamente.");
        }

        return RedirectToAction(nameof(Index));
    }

    // ELIMINAR: muestra la pantalla de confirmación
    [HttpGet]
    public virtual IActionResult Eliminar(int id)
    {
        var entidad = repositorio.ObtenerPorId(id);

        if (entidad == null)
        {
            return NotFound();
        }

        return View(entidad);
    }

    // ELIMINAR: confirma y da de baja (solo Administrador)
    [HttpPost]
    [ValidateAntiForgeryToken]
     [Authorize(Roles = "Administrador")]   
    public virtual IActionResult EliminarConfirmado(int id)
    {
        try
        {
            repositorio.Baja(id);
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            MostrarError("No se puede eliminar el registro porque tiene datos asociados.");
        }
        catch (MySqlException)
        {
            MostrarError("No se pudo eliminar el registro. Verifique los datos e inténtelo nuevamente.");
        }

        return RedirectToAction(nameof(Index));
    }
}