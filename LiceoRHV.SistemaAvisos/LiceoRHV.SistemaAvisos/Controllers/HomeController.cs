using LiceoRHV.SistemaAvisos.Data;
using LiceoRHV.SistemaAvisos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;

namespace LiceoRHV.SistemaAvisos.Controllers
{
    public class HomeController : Controller
    {
        private readonly LiceoRHVContext _context;

        public HomeController(LiceoRHVContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var ahora = DateTime.Now;
            var rolIdClaimStr = User.FindFirst("RolID")?.Value;
            int rolId2 = rolIdClaimStr != null ? int.Parse(rolIdClaimStr) : 0;
            var usuarioIdClaimStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioIdActual = usuarioIdClaimStr != null ? int.Parse(usuarioIdClaimStr) : 0;
            var eventos = _context.Eventos
    .Include(e => e.Rols)
    .Include(e => e.Inscripcions)
    .Include(e => e.ArchivoEventos)
    .Where(e => e.Estado == "Publicado" && e.Rols.Any(r => r.RolId == rolId2))
    .OrderBy(e => e.FechaEvento)
    .ToList();

            ViewBag.Eventos = eventos;
            ViewBag.UsuarioIdActual = usuarioIdActual;
            var paraPublicar = _context.Comunicacions
                .Where(c => c.Estado == "Borrador" && c.FechaPublicacion != null && c.FechaPublicacion <= ahora)
                .ToList();
            foreach (var c in paraPublicar) c.Estado = "Publicada";

            var paraVencer = _context.Comunicacions
                .Where(c => c.Estado == "Publicada" && c.FechaVencimiento != null && c.FechaVencimiento <= ahora)
                .ToList();
            foreach (var c in paraVencer) c.Estado = "Vencida";

            if (paraPublicar.Count > 0 || paraVencer.Count > 0)
                _context.SaveChanges();

            var rolIdClaim = User.FindFirst("RolID")?.Value;
            int rolId = rolIdClaim != null ? int.Parse(rolIdClaim) : 0;

            var comunicaciones = _context.Comunicacions
     .Include(c => c.Rols)
     .Include(c => c.Categoria)
     .Include(c => c.ArchivoComunicacions)
     .Include(c => c.CreadoPorUsuario)
     .Where(c => c.Estado == "Publicada" && c.Rols.Any(r => r.RolId == rolId))
     .OrderByDescending(c => c.Destacada)
     .ThenByDescending(c => c.FechaPublicacion ?? c.FechaCreacion)
     .ToList();

            return View(comunicaciones);
            

         
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}