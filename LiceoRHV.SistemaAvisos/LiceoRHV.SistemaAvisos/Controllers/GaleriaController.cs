using LiceoRHV.SistemaAvisos.Data;
using LiceoRHV.SistemaAvisos.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using LiceoRHV.SistemaAvisos.Services;

namespace LiceoRHV.SistemaAvisos.Controllers
{
    public class GaleriaController : Controller
    {
        private readonly LiceoRHVContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditoriaService _auditoria;

        public GaleriaController(LiceoRHVContext context, IWebHostEnvironment env, AuditoriaService auditoria)
        {
            _context = context;
            _env = env;
            _auditoria = auditoria;
        }

        private bool EsGestion()
        {
            var rolNombre = User.FindFirst("RolNombre")?.Value;
            return rolNombre == "Direccion" || rolNombre == "Administrativo";
        }

        public IActionResult Index(int? eventoId)
        {
            var query = _context.Fotografia
                .Include(f => f.Evento)
                .Include(f => f.SubidoPorUsuario)
                .AsQueryable();

            if (eventoId.HasValue)
                query = query.Where(f => f.EventoId == eventoId.Value);

            var fotos = query.OrderByDescending(f => f.FechaSubida).ToList();

            var agrupadas = fotos
                .GroupBy(f => f.Evento)
                .OrderByDescending(g => g.Key.FechaEvento)
                .ToList();

            ViewBag.PuedeGestionar = EsGestion();
            ViewBag.EventosFiltro = new SelectList(_context.Eventos.OrderByDescending(e => e.FechaEvento), "EventoId", "Titulo", eventoId);

            if (EsGestion())
            {
                ViewBag.EventosParaSubir = _context.Eventos.OrderByDescending(e => e.FechaEvento).ToList();
            }

            return View(agrupadas);
        }

        [HttpPost]
        public IActionResult Subir(int eventoId, List<IFormFile> fotos)
        {
            if (!EsGestion()) return RedirectToAction("Index");

            if (fotos == null || fotos.Count == 0)
            {
                TempData["MensajeGaleria"] = "Seleccioná al menos una fotografía.";
                return RedirectToAction("Index");
            }

            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId = usuarioIdClaim != null ? int.Parse(usuarioIdClaim) : 0;

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads", "galeria");
            Directory.CreateDirectory(carpetaDestino);

            int subidas = 0;
            foreach (var foto in fotos)
            {
                if (foto.Length == 0) continue;
                var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
                if (!extensionesPermitidas.Contains(extension)) continue;

                var nombreUnico = Guid.NewGuid().ToString() + extension;
                var rutaFisica = Path.Combine(carpetaDestino, nombreUnico);

                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                {
                    foto.CopyTo(stream);
                }

                _context.Fotografia.Add(new Fotografium
                {
                    EventoId = eventoId,
                    Archivo = "/uploads/galeria/" + nombreUnico,
                    SubidoPorUsuarioId = usuarioId,
                    FechaSubida = DateTime.Now
                });
                subidas++;
            }

            _context.SaveChanges();

            TempData["MensajeGaleria"] = subidas > 0
                ? $"Se subieron {subidas} fotografía(s) correctamente."
                : "No se pudo subir ninguna fotografía (formato no permitido).";
            var tituloEvento = _context.Eventos.Find(eventoId)?.Titulo ?? "evento";
            _auditoria.Registrar(User, "Galería", "Subir fotografías",
                $"Se subieron {subidas} fotografía(s) al evento '{tituloEvento}'.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            if (!EsGestion()) return RedirectToAction("Index");

            var foto = _context.Fotografia.FirstOrDefault(f => f.FotografiaId == id);
            if (foto == null) return NotFound();

            var rutaRelativa = foto.Archivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);
            if (System.IO.File.Exists(rutaFisica)) System.IO.File.Delete(rutaFisica);

            _context.Fotografia.Remove(foto);
            _context.SaveChanges();

            TempData["MensajeGaleria"] = "Fotografía eliminada correctamente.";
            _auditoria.Registrar(User, "Galería", "Eliminar fotografía",
    $"Se eliminó una fotografía del evento '{foto.Evento?.Titulo}'.");
            return RedirectToAction("Index");
        }
    }
}