using LiceoRHV.SistemaAvisos.Data;
using LiceoRHV.SistemaAvisos.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace LiceoRHV.SistemaAvisos.Controllers
{
    public class NormativaController : Controller
    {
        private readonly LiceoRHVContext _context;
        private readonly IWebHostEnvironment _env;

        public NormativaController(LiceoRHVContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool EsGestion()
        {
            var rolNombre = User.FindFirst("RolNombre")?.Value;
            return rolNombre == "Direccion" || rolNombre == "Administrativo";
        }

        public IActionResult Index()
        {
            var normativas = _context.NormativaInternas
                .Include(n => n.CreadoPorUsuario)
                .OrderByDescending(n => n.FechaActualizacion ?? n.FechaPublicacion)
                .ToList();

            ViewBag.PuedeGestionar = EsGestion();

            return View(normativas);
        }

        public IActionResult Create()
        {
            if (!EsGestion()) return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public IActionResult Create(string Titulo, string? Descripcion, IFormFile archivo)
        {
            if (!EsGestion()) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(Titulo))
            {
                ViewBag.ErrorNormativa = "El título es obligatorio.";
                return View();
            }

            if (archivo == null || archivo.Length == 0)
            {
                ViewBag.ErrorNormativa = "Tenés que adjuntar un archivo.";
                return View();
            }

            var extensionesPermitidas = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!extensionesPermitidas.Contains(extension))
            {
                ViewBag.ErrorNormativa = "Solo se permiten archivos PDF o Word.";
                return View();
            }

            var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads", "normativa");
            Directory.CreateDirectory(carpetaDestino);

            var nombreUnico = Guid.NewGuid().ToString() + extension;
            var rutaFisica = Path.Combine(carpetaDestino, nombreUnico);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }

            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId = usuarioIdClaim != null ? int.Parse(usuarioIdClaim) : 0;

            var normativa = new NormativaInterna
            {
                Titulo = Titulo,
                Descripcion = Descripcion,
                Archivo = "/uploads/normativa/" + nombreUnico,
                FechaPublicacion = DateTime.Now,
                CreadoPorUsuarioId = usuarioId
            };

            _context.NormativaInternas.Add(normativa);
            _context.SaveChanges();

            TempData["MensajeNormativa"] = "Normativa publicada correctamente.";
            return RedirectToAction("Index");
        }

        public IActionResult Editar(int id)
        {
            if (!EsGestion()) return RedirectToAction("Index");

            var normativa = _context.NormativaInternas.FirstOrDefault(n => n.NormativaId == id);
            if (normativa == null) return NotFound();

            return View(normativa);
        }

        [HttpPost]
        public IActionResult Editar(int NormativaId, string Titulo, string? Descripcion, IFormFile? archivo)
        {
            if (!EsGestion()) return RedirectToAction("Index");

            var normativa = _context.NormativaInternas.FirstOrDefault(n => n.NormativaId == NormativaId);
            if (normativa == null) return NotFound();

            if (string.IsNullOrWhiteSpace(Titulo))
            {
                ViewBag.ErrorNormativa = "El título es obligatorio.";
                return View(normativa);
            }

            normativa.Titulo = Titulo;
            normativa.Descripcion = Descripcion;
            normativa.FechaActualizacion = DateTime.Now;

            if (archivo != null && archivo.Length > 0)
            {
                var extensionesPermitidas = new[] { ".pdf", ".doc", ".docx" };
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (!extensionesPermitidas.Contains(extension))
                {
                    ViewBag.ErrorNormativa = "Solo se permiten archivos PDF o Word.";
                    return View(normativa);
                }

                var rutaViejaFisica = Path.Combine(_env.WebRootPath, normativa.Archivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(rutaViejaFisica))
                    System.IO.File.Delete(rutaViejaFisica);

                var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads", "normativa");
                Directory.CreateDirectory(carpetaDestino);

                var nombreUnico = Guid.NewGuid().ToString() + extension;
                var rutaFisica = Path.Combine(carpetaDestino, nombreUnico);

                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                {
                    archivo.CopyTo(stream);
                }

                normativa.Archivo = "/uploads/normativa/" + nombreUnico;
            }

            _context.SaveChanges();

            TempData["MensajeNormativa"] = "Normativa actualizada correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            if (!EsGestion()) return RedirectToAction("Index");

            var normativa = _context.NormativaInternas.FirstOrDefault(n => n.NormativaId == id);
            if (normativa == null) return NotFound();

            var rutaFisica = Path.Combine(_env.WebRootPath, normativa.Archivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(rutaFisica))
                System.IO.File.Delete(rutaFisica);

            _context.NormativaInternas.Remove(normativa);
            _context.SaveChanges();

            TempData["MensajeNormativa"] = "Normativa eliminada correctamente.";
            return RedirectToAction("Index");
        }
    }
}