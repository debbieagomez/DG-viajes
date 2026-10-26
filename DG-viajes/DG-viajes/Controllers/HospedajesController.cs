using DG_viajes.Models;
using DG_viajes.Repositories;
using DG_viajes.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

public class HospedajesController : Controller
{
    private static readonly string[] Extensiones = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024;
    private const string RutaFotos = "/uploads/hospedajes/";

    private readonly RepositorioHospedaje repo;
    private readonly IWebHostEnvironment env;

    public HospedajesController(IConfiguration config, IWebHostEnvironment env)
    {
        repo = new RepositorioHospedaje(config);
        this.env = env;
    }

    public IActionResult Index() => View();

    // ---------- JSON para Vue ----------
    [HttpGet]
    public IActionResult Listar([FromQuery] HospedajeFiltro filtro) => Json(repo.Listar(filtro));

    [HttpGet]
    public IActionResult Opciones() => Json(new { destinos = repo.Destinos(), tipos = repo.Tipos() });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Guardar([FromBody] Hospedaje? h)
    {
        if (h == null) return BadRequest(new { errores = new[] { "No se recibieron datos." } });

        Hospedaje? actual = h.Id > 0 ? repo.Obtener(h.Id) : null;
        if (h.Id > 0 && actual == null)
            return NotFound(new { errores = new[] { "El hospedaje no existe." } });

        var errores = Validar(h, actual);
        if (errores.Count > 0) return BadRequest(new { errores });

        if (h.Id == 0) h.Id = repo.Alta(h);
        else repo.Modificar(h);
        return Json(new { ok = true, id = h.Id });
    }

    // Desactivar no toca las reservas existentes; solo impide usarlo en reservas nuevas.
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CambiarActivo(int id, bool activo)
    {
        if (repo.Obtener(id) == null) return NotFound(new { errores = new[] { "El hospedaje no existe." } });
        repo.CambiarActivo(id, activo);
        return Json(new { ok = true });
    }

    // TODO: restringir a administradores con [Authorize(Roles = "Administrador")] cuando esté el login.
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        if (repo.Obtener(id) == null) return NotFound(new { errores = new[] { "El hospedaje no existe." } });
        if (repo.TieneReservas(id))
            return Conflict(new { errores = new[] { "Tiene reservas asociadas. Desactivalo en lugar de eliminarlo." } });

        var fotos = repo.Fotos(id);
        repo.Eliminar(id);
        foreach (var f in fotos) BorrarArchivo(f.Archivo);
        return Json(new { ok = true });
    }

    // ---------- Fotos ----------
    [HttpGet]
    public IActionResult Fotos(int id) => Json(repo.Fotos(id));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirFoto(int id, List<IFormFile> archivos)
    {
        if (repo.Obtener(id) == null) return NotFound(new { errores = new[] { "El hospedaje no existe." } });
        if (archivos == null || archivos.Count == 0)
            return BadRequest(new { errores = new[] { "Elegí al menos una imagen." } });

        var carpeta = Path.Combine(env.WebRootPath, "uploads", "hospedajes");
        Directory.CreateDirectory(carpeta);

        var errores = new List<string>();
        int subidas = 0;
        foreach (var a in archivos)
        {
            var ext = Path.GetExtension(a.FileName).ToLowerInvariant();
            if (!Extensiones.Contains(ext)) { errores.Add($"{a.FileName}: solo se aceptan JPG, PNG o WEBP."); continue; }
            if (a.Length == 0 || a.Length > MaxBytes) { errores.Add($"{a.FileName}: el tamaño máximo es 5 MB."); continue; }
            using (var s = a.OpenReadStream())
            {
                if (!EsImagen(s, ext)) { errores.Add($"{a.FileName}: el archivo no es una imagen válida."); continue; }
            }

            var nombre = $"{id}_{Guid.NewGuid():N}{ext}";
            using (var fs = new FileStream(Path.Combine(carpeta, nombre), FileMode.Create))
                await a.CopyToAsync(fs);
            repo.AltaFoto(id, RutaFotos + nombre);
            subidas++;
        }
        return Json(new { subidas, errores });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult MarcarPortada(int id)
    {
        if (!repo.MarcarPortada(id)) return NotFound(new { errores = new[] { "La foto no existe." } });
        return Json(new { ok = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EliminarFoto(int id)
    {
        var archivo = repo.EliminarFoto(id);
        if (archivo == null) return NotFound(new { errores = new[] { "La foto no existe." } });
        BorrarArchivo(archivo);
        return Json(new { ok = true });
    }

    // ---------- Auxiliares ----------
    private List<string> Validar(Hospedaje h, Hospedaje? actual)
    {
        var e = new List<string>();
        h.Nombre = (h.Nombre ?? "").Trim();
        h.Direccion = string.IsNullOrWhiteSpace(h.Direccion) ? null : h.Direccion.Trim();
        h.Servicios = string.IsNullOrWhiteSpace(h.Servicios) ? null : h.Servicios.Trim();

        if (h.Nombre.Length == 0) e.Add("El nombre es obligatorio.");
        else if (h.Nombre.Length > 120) e.Add("El nombre no puede superar los 120 caracteres.");
        if (h.Direccion?.Length > 200) e.Add("La dirección no puede superar los 200 caracteres.");
        if (h.Servicios?.Length > 500) e.Add("Los servicios no pueden superar los 500 caracteres.");
        if (h.CapacidadMaxima <= 0) e.Add("La capacidad máxima debe ser mayor a cero.");
        if (h.PrecioNoche < 0 || h.PrecioNoche > 9999999999m) e.Add("El precio por noche no es válido.");
        if (!repo.DestinoPermitido(h.DestinoId, actual?.DestinoId ?? 0)) e.Add("Elegí un destino activo.");
        if (!repo.TipoExiste(h.TipoId)) e.Add("Elegí un tipo de hospedaje válido.");
        return e;
    }

    // Verifica la firma del archivo, no solo la extensión.
    private static bool EsImagen(Stream s, string ext)
    {
        var b = new byte[12];
        if (s.Read(b, 0, 12) < 12) return false;
        return ext switch
        {
            ".jpg" or ".jpeg" => b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
            ".png" => b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47,
            ".webp" => b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
                       && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50,
            _ => false
        };
    }

    private void BorrarArchivo(string archivo)
    {
        var raiz = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads", "hospedajes")) + Path.DirectorySeparatorChar;
        var ruta = Path.GetFullPath(Path.Combine(env.WebRootPath,
            archivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (ruta.StartsWith(raiz) && System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);
    }
}
