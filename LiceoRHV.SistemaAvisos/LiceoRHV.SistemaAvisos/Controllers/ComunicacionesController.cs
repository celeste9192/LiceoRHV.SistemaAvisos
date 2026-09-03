using System.Linq;
using System.Security.Claims;
using LiceoRHV.SistemaAvisos.Data;
using LiceoRHV.SistemaAvisos.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LiceoRHV.SistemaAvisos.Services;

namespace LiceoRHV.SistemaAvisos.Controllers
{
    public class ComunicacionesController : Controller
    {
        private readonly LiceoRHVContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditoriaService _auditoria;

        public ComunicacionesController(LiceoRHVContext context, IWebHostEnvironment env, AuditoriaService auditoria)
        {
            _context = context;
            _env = env;
            _auditoria = auditoria;
        }

        private void ActualizarEstadosAutomaticos()
        {
            var ahora = DateTime.Now;

            var paraPublicar = _context.Comunicacions
                .Where(c => c.Estado == "Borrador" && c.FechaPublicacion != null && c.FechaPublicacion <= ahora)
                .ToList();
            foreach (var c in paraPublicar)
            {
                c.Estado = "Publicada";
                _auditoria.RegistrarAutomatico("Comunicaciones", "Publicación automática",
                    $"Se publicó automáticamente la comunicación '{c.Titulo}' según la fecha programada.");
            }

            var paraVencer = _context.Comunicacions
                .Where(c => c.Estado == "Publicada" && c.FechaVencimiento != null && c.FechaVencimiento <= ahora)
                .ToList();
            foreach (var c in paraVencer) c.Estado = "Vencida";

            if (paraPublicar.Count > 0 || paraVencer.Count > 0)
                _context.SaveChanges();
        }

        // GET: Comunicaciones
        public IActionResult Index(string? estado, string? tipo, int? rolId, int? categoriaId, string? titulo)
        {
            ActualizarEstadosAutomaticos();

            var query = _context.Comunicacions
                .Include(c => c.Rols)
                .Include(c => c.Categoria)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(c => c.Estado == estado);

            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(c => c.Tipo == tipo);

            if (rolId.HasValue)
                query = query.Where(c => c.Rols.Any(r => r.RolId == rolId.Value));

            if (categoriaId.HasValue)
                query = query.Where(c => c.Categoria.Any(cat => cat.CategoriaId == categoriaId.Value));

            if (!string.IsNullOrWhiteSpace(titulo))
                query = query.Where(c => c.Titulo.Contains(titulo));

            var comunicaciones = query.OrderByDescending(c => c.FechaCreacion).ToList();

            ViewBag.RolesFiltro = new SelectList(_context.Rols, "RolId", "NombreRol", rolId);
            ViewBag.CategoriasFiltro = new SelectList(_context.Categoria, "CategoriaId", "NombreCategoria", categoriaId);
            ViewBag.FiltroEstado = estado ?? "";
            ViewBag.FiltroTipo = tipo ?? "";
            ViewBag.FiltroTitulo = titulo ?? "";

            return View(comunicaciones);
        }

        // GET: Comunicaciones/Create
        public IActionResult Create()
        {
            ViewBag.Roles = _context.Rols.ToList();
            ViewBag.Categorias = _context.Categoria.ToList();
            return View();
        }

        // POST: Comunicaciones/Create
        [HttpPost]
        public IActionResult Create(
            string Titulo, string Contenido, string Tipo,
            List<int> rolesSeleccionados, List<int>? categoriasSeleccionadas,
            DateTime? FechaPublicacion, DateTime? FechaVencimiento,
            List<IFormFile>? archivos)
        {
            ViewBag.Roles = _context.Rols.ToList();
            ViewBag.Categorias = _context.Categoria.ToList();

            if (string.IsNullOrWhiteSpace(Titulo) || string.IsNullOrWhiteSpace(Contenido))
            {
                ViewBag.ErrorComunicacion = "El título y el contenido son obligatorios.";
                return View();
            }

            if (rolesSeleccionados == null || rolesSeleccionados.Count == 0)
            {
                ViewBag.ErrorComunicacion = "Seleccioná al menos un grupo destinatario.";
                return View();
            }

            if (FechaPublicacion != null && FechaVencimiento != null && FechaVencimiento < FechaPublicacion)
            {
                ViewBag.ErrorComunicacion = "La fecha de vencimiento no puede ser anterior a la fecha de publicación.";
                return View();
            }

            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId = usuarioIdClaim != null ? int.Parse(usuarioIdClaim) : 0;

            var comunicacion = new Comunicacion
            {
                Titulo = Titulo,
                Contenido = Contenido,
                Tipo = Tipo,
                Estado = "Borrador",
                Destacada = false,
                FechaPublicacion = FechaPublicacion,
                FechaVencimiento = FechaVencimiento,
                CreadoPorUsuarioId = usuarioId,
                FechaCreacion = DateTime.Now
            };

            var rolesElegidos = _context.Rols.Where(r => rolesSeleccionados.Contains(r.RolId)).ToList();
            foreach (var rol in rolesElegidos)
                comunicacion.Rols.Add(rol);

            if (categoriasSeleccionadas != null && categoriasSeleccionadas.Count > 0)
            {
                var categoriasElegidas = _context.Categoria.Where(c => categoriasSeleccionadas.Contains(c.CategoriaId)).ToList();
                foreach (var cat in categoriasElegidas)
                    comunicacion.Categoria.Add(cat);
            }

            _context.Comunicacions.Add(comunicacion);
            _context.SaveChanges();

            if (archivos != null && archivos.Count > 0)
            {
                var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
                var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads", "comunicaciones");
                Directory.CreateDirectory(carpetaDestino);

                foreach (var archivo in archivos)
                {
                    if (archivo.Length == 0) continue;

                    var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                    if (!extensionesPermitidas.Contains(extension)) continue;

                    var nombreUnico = Guid.NewGuid().ToString() + extension;
                    var rutaFisica = Path.Combine(carpetaDestino, nombreUnico);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        archivo.CopyTo(stream);
                    }

                    _context.ArchivoComunicacions.Add(new ArchivoComunicacion
                    {
                        ComunicacionId = comunicacion.ComunicacionId,
                        NombreArchivo = archivo.FileName,
                        Ruta = "/uploads/comunicaciones/" + nombreUnico,
                        TipoArchivo = archivo.ContentType,
                        TamanoKb = (int)(archivo.Length / 1024),
                        FechaSubida = DateTime.Now
                    });
                }
                _context.SaveChanges();
            }

            TempData["MensajeComunicacion"] = "Comunicación creada como borrador.";
            _auditoria.Registrar(User, "Comunicaciones", "Crear",
    $"Se creó la comunicación '{comunicacion.Titulo}' ({comunicacion.Tipo}).");
            return RedirectToAction("Index");
        }

        // GET: Comunicaciones/Detalle/5
        public IActionResult Detalle(int id)
        {
            var comunicacion = _context.Comunicacions
                .Include(c => c.Rols)
                .Include(c => c.Categoria)
                .Include(c => c.ArchivoComunicacions)
                .Include(c => c.CreadoPorUsuario)
                .FirstOrDefault(c => c.ComunicacionId == id);

            if (comunicacion == null)
                return NotFound();

            ViewBag.Roles = _context.Rols.ToList();
            ViewBag.Categorias = _context.Categoria.ToList();

            return PartialView("_DetalleComunicacion", comunicacion);
        }

        // POST: Comunicaciones/EditarComunicacion
        [HttpPost]
        public IActionResult EditarComunicacion(
            int ComunicacionId, string Titulo, string Contenido, string Tipo,
            DateTime? FechaPublicacion, DateTime? FechaVencimiento,
            List<int> rolesSeleccionados, List<int>? categoriasSeleccionadas)
        {
            var comunicacion = _context.Comunicacions
                .Include(c => c.Rols)
                .Include(c => c.Categoria)
                .FirstOrDefault(c => c.ComunicacionId == ComunicacionId);

            if (comunicacion == null)
                return NotFound();

            comunicacion.Titulo = Titulo;
            comunicacion.Contenido = Contenido;
            comunicacion.Tipo = Tipo;
            comunicacion.FechaPublicacion = FechaPublicacion;
            comunicacion.FechaVencimiento = FechaVencimiento;

            var ahora = DateTime.Now;
            if (FechaVencimiento != null && FechaVencimiento <= ahora)
            {
                comunicacion.Estado = "Vencida";
            }
            else if (FechaPublicacion == null || FechaPublicacion <= ahora)
            {
                comunicacion.Estado = "Publicada";
            }
            else
            {
                comunicacion.Estado = "Borrador";
            }

            comunicacion.Rols.Clear();
            if (rolesSeleccionados != null)
            {
                var rolesElegidos = _context.Rols.Where(r => rolesSeleccionados.Contains(r.RolId)).ToList();
                foreach (var rol in rolesElegidos) comunicacion.Rols.Add(rol);
            }

            comunicacion.Categoria.Clear();
            if (categoriasSeleccionadas != null)
            {
                var categoriasElegidas = _context.Categoria.Where(c => categoriasSeleccionadas.Contains(c.CategoriaId)).ToList();
                foreach (var cat in categoriasElegidas) comunicacion.Categoria.Add(cat);
            }

            _context.SaveChanges();

            TempData["MensajeComunicacion"] = "Comunicación actualizada correctamente.";
            _auditoria.Registrar(User, "Comunicaciones", "Editar",
    $"Se editó la comunicación '{comunicacion.Titulo}'.");
            return RedirectToAction("Index");
        }

        // POST: Comunicaciones/ToggleDestacada
        [HttpPost]
        public IActionResult ToggleDestacada(int id)
        {
            var comunicacion = _context.Comunicacions.FirstOrDefault(c => c.ComunicacionId == id);
            if (comunicacion == null) return NotFound();

            comunicacion.Destacada = !comunicacion.Destacada;
            _context.SaveChanges();
            _auditoria.Registrar(User, "Comunicaciones", "Destacar",
    $"Se {(comunicacion.Destacada ? "marcó" : "quitó")} como destacada la comunicación '{comunicacion.Titulo}'.");
            return RedirectToAction("Index");
        }

        // POST: Comunicaciones/Eliminar
        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            var comunicacion = _context.Comunicacions
                .Include(c => c.ArchivoComunicacions)
                .FirstOrDefault(c => c.ComunicacionId == id);

            if (comunicacion == null) return NotFound();

            foreach (var archivo in comunicacion.ArchivoComunicacions)
            {
                var rutaRelativa = archivo.Ruta.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);

                if (System.IO.File.Exists(rutaFisica))
                {
                    System.IO.File.Delete(rutaFisica);
                }
            }

            _context.Comunicacions.Remove(comunicacion);
            _context.SaveChanges();

            TempData["MensajeComunicacion"] = "Comunicación eliminada correctamente.";
            _auditoria.Registrar(User, "Comunicaciones", "Eliminar",
    $"Se eliminó la comunicación '{comunicacion.Titulo}'.");

            return RedirectToAction("Index");
        }

        // POST: Comunicaciones/CrearCategoriaRapida
        [HttpPost]
        public IActionResult CrearCategoriaRapida(string nombreCategoria)
        {
            if (string.IsNullOrWhiteSpace(nombreCategoria))
                return BadRequest();

            var existente = _context.Categoria.FirstOrDefault(c => c.NombreCategoria == nombreCategoria);
            if (existente != null)
                return Json(new { id = existente.CategoriaId, nombre = existente.NombreCategoria });

            var nueva = new Categorium { NombreCategoria = nombreCategoria };
            _context.Categoria.Add(nueva);
            _context.SaveChanges();

            return Json(new { id = nueva.CategoriaId, nombre = nueva.NombreCategoria });
        }
    }
}