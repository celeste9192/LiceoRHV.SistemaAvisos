using LiceoRHV.SistemaAvisos.Data;
using LiceoRHV.SistemaAvisos.Models;
using System.Security.Claims;

namespace LiceoRHV.SistemaAvisos.Services
{
    public class AuditoriaService
    {
        private readonly LiceoRHVContext _context;

        public AuditoriaService(LiceoRHVContext context)
        {
            _context = context;
        }

        public void Registrar(ClaimsPrincipal user, string modulo, string accion, string descripcion)
        {
            var usuarioIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? usuarioId = usuarioIdClaim != null ? int.Parse(usuarioIdClaim) : (int?)null;

            _context.RegistroAuditoria.Add(new RegistroAuditorium
            {
                UsuarioId = usuarioId,
                Modulo = modulo,
                Accion = accion,
                Descripcion = descripcion,
                FechaHora = DateTime.Now
            });
            _context.SaveChanges();
        }

        // Sobrecarga para acciones automáticas del sistema (sin usuario logueado, ej. publicación programada)
        public void RegistrarAutomatico(string modulo, string accion, string descripcion)
        {
            _context.RegistroAuditoria.Add(new RegistroAuditorium
            {
                UsuarioId = null,
                Modulo = modulo,
                Accion = accion,
                Descripcion = descripcion,
                FechaHora = DateTime.Now
            });
            _context.SaveChanges();
        }
    }
}