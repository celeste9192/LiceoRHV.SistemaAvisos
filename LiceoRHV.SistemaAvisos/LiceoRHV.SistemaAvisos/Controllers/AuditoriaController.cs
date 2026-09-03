using LiceoRHV.SistemaAvisos.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LiceoRHV.SistemaAvisos.Controllers
{
    public class AuditoriaController : Controller
    {
        private readonly LiceoRHVContext _context;

        public AuditoriaController(LiceoRHVContext context)
        {
            _context = context;
        }

        private bool EsGestion()
        {
            var rolNombre = User.FindFirst("RolNombre")?.Value;
            return rolNombre == "Direccion" || rolNombre == "Administrativo";
        }

        public IActionResult Index(int? usuarioId, string? modulo, string? accion, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");

            var query = _context.RegistroAuditoria
                .Include(r => r.Usuario)
                .AsQueryable();

            if (usuarioId.HasValue)
                query = query.Where(r => r.UsuarioId == usuarioId.Value);

            if (!string.IsNullOrWhiteSpace(modulo))
                query = query.Where(r => r.Modulo == modulo);

            if (!string.IsNullOrWhiteSpace(accion))
                query = query.Where(r => r.Accion.Contains(accion));

            if (fechaDesde.HasValue)
                query = query.Where(r => r.FechaHora >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(r => r.FechaHora <= fechaHasta.Value.AddDays(1).AddTicks(-1));

            var registros = query.OrderByDescending(r => r.FechaHora).ToList();

            ViewBag.UsuariosFiltro = new SelectList(_context.Usuarios.OrderBy(u => u.Nombre), "UsuarioId", "Nombre", usuarioId);
            ViewBag.ModulosFiltro = _context.RegistroAuditoria.Select(r => r.Modulo).Distinct().ToList();
            ViewBag.FiltroModulo = modulo ?? "";
            ViewBag.FiltroAccion = accion ?? "";
            ViewBag.FiltroFechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FiltroFechaHasta = fechaHasta?.ToString("yyyy-MM-dd");

            return View(registros);
        }
    }
}