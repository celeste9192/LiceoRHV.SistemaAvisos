using System.Security.Claims;
using LiceoRHV.SistemaAvisos.Data; // ajustá al namespace real de tu proyecto
using LiceoRHV.SistemaAvisos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LiceoRHV.SistemaAvisos.Services; // agregar arriba



namespace LiceoRHV.SistemaAvisos.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly LiceoRHVContext _context;
        private readonly PasswordHasher<Usuario> _hasher = new PasswordHasher<Usuario>();
        private readonly AuditoriaService _auditoria; // agregar el campo

    public UsuariosController(LiceoRHVContext context, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }
        public IActionResult Index(string? estado, int? rolId, string? cedula)
        {
            var query = _context.Usuarios.Include(u => u.Rol).AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(u => u.Estado == estado);

            if (rolId.HasValue)
                query = query.Where(u => u.RolId == rolId.Value);

            if (!string.IsNullOrWhiteSpace(cedula))
                query = query.Where(u => u.Cedula.Contains(cedula));

            var usuarios = query.ToList();

            ViewBag.Roles = new SelectList(_context.Rols, "RolId", "NombreRol");
            ViewBag.RolesFiltro = new SelectList(_context.Rols, "RolId", "NombreRol", rolId);
            ViewBag.FiltroEstado = estado ?? "";
            ViewBag.FiltroCedula = cedula ?? "";

            return View(usuarios);
        }

        // POST: Usuarios/CrearDesdeGestion  (creación directa por Dirección/Administrativo — RF-USR-03)
        [HttpPost]
        public IActionResult CrearDesdeGestion(Usuario usuario)
        {
            bool correoExiste = _context.Usuarios.Any(u => u.Correo == usuario.Correo);
            bool cedulaExiste = _context.Usuarios.Any(u => u.Cedula == usuario.Cedula);


            if (correoExiste || cedulaExiste)
            {
                if (correoExiste && cedulaExiste)
                    ViewBag.ErrorModal = "Ya existe un usuario registrado con ese correo y esa cédula.";
                else if (correoExiste)
                    ViewBag.ErrorModal = "Ya existe un usuario registrado con ese correo.";
                else
                    ViewBag.ErrorModal = "Ya existe un usuario registrado con esa cédula.";

                ViewBag.ReabrirModal = true;
                ViewBag.UsuarioForm = usuario;
                ViewBag.Roles = new SelectList(_context.Rols, "RolId", "NombreRol", usuario.RolId);

                var listaConError = _context.Usuarios.Include(u => u.Rol).ToList();
                _auditoria.Registrar(User, "Usuarios", "Crear",
    $"Se creó el usuario {usuario.Nombre} ({usuario.Correo}) con rol {usuario.RolId}, estado Activa.");
                return View("Index", listaConError);
                
            }

            usuario.Estado = "Activa";
            usuario.FechaRegistro = DateTime.Now;
            usuario.PasswordHash = _hasher.HashPassword(usuario, usuario.PasswordHash);
            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Usuarios/Detalle/5
        public IActionResult Detalle(int id)
        {
            var usuario = _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefault(u => u.UsuarioId == id);

            if (usuario == null)
                return NotFound();

            ViewBag.Roles = new SelectList(_context.Rols, "RolId", "NombreRol", usuario.RolId);
            return PartialView("_DetalleUsuario", usuario);
        }

        // POST: Usuarios/EditarUsuario
        [HttpPost]
        public IActionResult EditarUsuario(int UsuarioId, string Nombre, string Cedula, string Correo, string? Telefono, int RolId)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.UsuarioId == UsuarioId);
            if (usuario == null) return NotFound();

            bool correoDuplicado = _context.Usuarios.Any(u => u.Correo == Correo && u.UsuarioId != UsuarioId);
            bool cedulaDuplicada = _context.Usuarios.Any(u => u.Cedula == Cedula && u.UsuarioId != UsuarioId);

            if (correoDuplicado || cedulaDuplicada)
            {
                TempData["ErrorEdicion"] = correoDuplicado
                    ? "Ya existe otro usuario con ese correo."
                    : "Ya existe otro usuario con esa cédula.";
                return RedirectToAction("Index");
            }

            usuario.Nombre = Nombre;
            usuario.Cedula = Cedula;
            usuario.Correo = Correo;
            usuario.Telefono = Telefono;
            usuario.RolId = RolId;

            _context.SaveChanges();
            _auditoria.Registrar(User, "Usuarios", "Editar",
    $"Se editó la información del usuario {usuario.Nombre} ({usuario.Correo}).");
            return RedirectToAction("Index");
        }

        // POST: Usuarios/CambiarEstado
        [HttpPost]
        public IActionResult CambiarEstado(int id, string nuevoEstado)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.UsuarioId == id);
            if (usuario == null) return NotFound();

            usuario.Estado = nuevoEstado;

            if (nuevoEstado == "Activa")
            {
                var revisorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (revisorIdClaim != null)
                {
                    usuario.RevisadoPorUsuarioId = int.Parse(revisorIdClaim);
                }
                usuario.FechaRevision = DateTime.Now;
                usuario.MotivoRechazo = null;
            }

            _context.SaveChanges();
            _auditoria.Registrar(User, "Usuarios", nuevoEstado == "Activa" ? "Activar" : "Inactivar",
    $"Se cambió el estado del usuario {usuario.Nombre} a {nuevoEstado}.");
            return RedirectToAction("Index");
        }

        // POST: Usuarios/Rechazar
        [HttpPost]
        public IActionResult Rechazar(int id, string motivo)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.UsuarioId == id);
            if (usuario == null) return NotFound();

            usuario.Estado = "Rechazada";
            usuario.MotivoRechazo = motivo;

            var revisorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (revisorIdClaim != null)
            {
                usuario.RevisadoPorUsuarioId = int.Parse(revisorIdClaim);
            }
            usuario.FechaRevision = DateTime.Now;

            _context.SaveChanges();
            _auditoria.Registrar(User, "Usuarios", "Rechazar",
    $"Se rechazó la cuenta de {usuario.Nombre}. Motivo: {motivo}");
            return RedirectToAction("Index");
        }


        // GET: Usuarios/Create
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(_context.Rols, "RolId", "NombreRol");
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        public IActionResult Create(Usuario usuario)
        {
            usuario.Estado = "Pendiente";       // todo autorregistro nace como Pendiente (RF-USR-01)
            usuario.FechaRegistro = DateTime.Now;

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}