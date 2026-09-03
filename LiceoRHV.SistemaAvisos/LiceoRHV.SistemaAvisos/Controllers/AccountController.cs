using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using LiceoRHV.SistemaAvisos.Data; // ajustá al namespace real de tu proyecto
using LiceoRHV.SistemaAvisos.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LiceoRHV.SistemaAvisos.Services;

namespace LiceoRHV.SistemaAvisos.Controllers
{
    public class AccountController : Controller
    {
        private readonly LiceoRHVContext _context;
        private readonly AuditoriaService _auditoria;




        private readonly IConfiguration _config;

        public AccountController(LiceoRHVContext context, IConfiguration config, AuditoriaService auditoria)
        {
            _context = context;
            _config = config;
            _auditoria = auditoria;
        }

        private void EnviarCorreo(string destinatario, string asunto, string cuerpo)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]);
            var usuarioSmtp = _config["EmailSettings:Usuario"];
            var passwordSmtp = _config["EmailSettings:Password"];
            var nombreRemitente = _config["EmailSettings:NombreRemitente"];

            using var mensaje = new MailMessage();
            mensaje.From = new MailAddress(usuarioSmtp, nombreRemitente);
            mensaje.To.Add(destinatario);
            mensaje.Subject = asunto;
            mensaje.Body = cuerpo;
            mensaje.IsBodyHtml = false;

            using var cliente = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new System.Net.NetworkCredential(usuarioSmtp, passwordSmtp),
                EnableSsl = true
            };

            cliente.Send(mensaje);
        }

        [HttpPost]
        public IActionResult SolicitarRecuperacion(string correo)
        {
            ViewBag.MostrarRecuperar = true;

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correo);

            if (usuario != null)
            {
                var codigo = new Random().Next(100000, 999999).ToString();
                usuario.CodigoRecuperacion = codigo;
                usuario.CodigoRecuperacionExpira = DateTime.Now.AddMinutes(15);
                _context.SaveChanges();

                try
                {
                    EnviarCorreo(usuario.Correo,
                        "Código de recuperación - Liceo Rodrigo Hernández Vargas",
                        $"Hola {usuario.Nombre},\n\nTu código de verificación es: {codigo}\n\nEste código vence en 15 minutos.\n\nSi no solicitaste este cambio, ignorá este mensaje.");
                }
                catch
                {
                    ViewBag.ErrorRecuperar = "No se pudo enviar el correo. Intentá de nuevo más tarde.";
                    ViewBag.PasoRecuperar = 1;
                    return View("Login");
                }
            }

            ViewBag.MensajeRecuperar = "Si el correo está registrado, te enviamos un código de verificación.";
            ViewBag.PasoRecuperar = 2;
            ViewBag.CorreoRecuperar = correo;
            return View("Login");
        }

        [HttpPost]
        public IActionResult ConfirmarRecuperacion(string correo, string codigo, string nuevaPassword, string confirmarPassword)
        {
            ViewBag.MostrarRecuperar = true;
            ViewBag.PasoRecuperar = 2;
            ViewBag.CorreoRecuperar = correo;

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correo);

            if (usuario == null || usuario.CodigoRecuperacion != codigo || usuario.CodigoRecuperacionExpira < DateTime.Now)
            {
                ViewBag.ErrorRecuperar = "El código no es válido o expiró. Solicitá uno nuevo.";
                return View("Login");
            }

            if (string.IsNullOrEmpty(nuevaPassword) || nuevaPassword.Length < 8)
            {
                ViewBag.ErrorRecuperar = "La contraseña debe tener al menos 8 caracteres.";
                return View("Login");
            }

            if (nuevaPassword != confirmarPassword)
            {
                ViewBag.ErrorRecuperar = "Las contraseñas no coinciden.";
                return View("Login");
            }

            usuario.PasswordHash = _hasher.HashPassword(usuario, nuevaPassword);
            usuario.CodigoRecuperacion = null;
            usuario.CodigoRecuperacionExpira = null;
            _context.SaveChanges();

            TempData["RegistroExitoso"] = "Tu contraseña fue actualizada. Ya podés iniciar sesión.";
            return RedirectToAction("Login");
        }
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.RolEstudianteId = _context.Rols.First(r => r.NombreRol == "Estudiante").RolId;
            ViewBag.RolPadreId = _context.Rols.First(r => r.NombreRol == "Padre/Tutor").RolId;
          

            return View();
        }

        [HttpPost]
        public IActionResult Registro(Usuario usuario, string confirmarPassword)
        {
            ViewBag.RolEstudianteId = _context.Rols.First(r => r.NombreRol == "Estudiante").RolId;
            ViewBag.RolPadreId = _context.Rols.First(r => r.NombreRol == "Padre/Tutor").RolId;
            ViewBag.MostrarRegistro = true;
            ViewBag.UsuarioRegistro = usuario;

            if (string.IsNullOrEmpty(usuario.PasswordHash) || usuario.PasswordHash.Length < 8)
            {
                ViewBag.ErrorRegistro = "La contraseña debe tener al menos 8 caracteres.";
                return View("Login");
            }

            if (usuario.PasswordHash != confirmarPassword)
            {
                ViewBag.ErrorRegistro = "Las contraseñas no coinciden.";
                return View("Login");
            }

            bool correoExiste = _context.Usuarios.Any(u => u.Correo == usuario.Correo);
            bool cedulaExiste = _context.Usuarios.Any(u => u.Cedula == usuario.Cedula);

            if (correoExiste || cedulaExiste)
            {
                ViewBag.ErrorRegistro = correoExiste
                    ? "Ya existe una cuenta registrada con ese correo."
                    : "Ya existe una cuenta registrada con esa cédula.";
                return View("Login");
            }

            usuario.Estado = "Pendiente";
            usuario.FechaRegistro = DateTime.Now;
            usuario.PasswordHash = _hasher.HashPassword(usuario, usuario.PasswordHash);
            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            TempData["RegistroExitoso"] = "¡Listo! Tu cuenta fue creada y está pendiente de aprobación. Te avisaremos cuando puedas ingresar.";
            _auditoria.Registrar(User, "Usuarios", "Registro",
    $"Se registró una nueva cuenta pendiente: {usuario.Nombre} ({usuario.Correo}).");
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string correo, string password, bool recordarme)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correo);

            if (usuario == null || !VerificarPassword(usuario, password, out bool necesitaRehash))
            {
                ViewBag.Error = "Correo o contraseña incorrectos.";
                return View();
            }

            if (necesitaRehash)
            {
                usuario.PasswordHash = _hasher.HashPassword(usuario, password);
                _context.SaveChanges();
            }

            if (usuario.Estado == "Rechazada")
            {
                ViewBag.Error = "Tu solicitud fue rechazada. Motivo: " + usuario.MotivoRechazo;
                return View();
            }

            if (usuario.Estado != "Activa")
            {
                ViewBag.Error = "Tu cuenta todavía no está activa. Esperá la aprobación de Dirección.";
                return View();
            }

            var rol = _context.Rols.FirstOrDefault(r => r.RolId == usuario.RolId);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
        new Claim(ClaimTypes.Name, usuario.Nombre),
        new Claim("RolID", usuario.RolId.ToString()),
        new Claim("RolNombre", rol != null ? rol.NombreRol : "")
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = recordarme,
                ExpiresUtc = recordarme ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [Authorize] 
        public IActionResult Perfil()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (usuarioIdClaim == null)
                return RedirectToAction("Login");

            var usuario = _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefault(u => u.UsuarioId == int.Parse(usuarioIdClaim));

            if (usuario == null)
                return RedirectToAction("Login");

            return View(usuario);
        }

        [HttpPost]
        public IActionResult EditarPerfil(string Nombre, string Cedula, string Correo, string Telefono)
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (usuarioIdClaim == null) return RedirectToAction("Login");
            int usuarioId = int.Parse(usuarioIdClaim);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId);
            if (usuario == null) return RedirectToAction("Login");

            bool correoExiste = _context.Usuarios.Any(u => u.Correo == Correo && u.UsuarioId != usuarioId);
            bool cedulaExiste = _context.Usuarios.Any(u => u.Cedula == Cedula && u.UsuarioId != usuarioId);

            if (correoExiste || cedulaExiste)
            {
                TempData["ErrorPerfil"] = correoExiste
                    ? "Ya existe otra cuenta con ese correo."
                    : "Ya existe otra cuenta con esa cédula.";
                _auditoria.Registrar(User, "Usuarios", "Editar",
    $"{usuario.Nombre} actualizó su propia información de perfil.");
                return RedirectToAction("Perfil");
            }

            usuario.Nombre = Nombre;
            usuario.Cedula = Cedula;
            usuario.Correo = Correo;
            usuario.Telefono = Telefono;
            _context.SaveChanges();

            TempData["MensajePerfil"] = "Tus datos fueron actualizados correctamente. El nombre en el menú se actualiza la próxima vez que inicies sesión.";
            return RedirectToAction("Perfil");
        }

        [HttpPost]
        public IActionResult CambiarPassword(string passwordActual, string nuevaPassword, string confirmarPassword)
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (usuarioIdClaim == null) return RedirectToAction("Login");
            int usuarioId = int.Parse(usuarioIdClaim);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId);
            if (usuario == null) return RedirectToAction("Login");

            if (!VerificarPassword(usuario, passwordActual, out _))
            {
                TempData["ErrorPassword"] = "La contraseña actual no es correcta.";
                return RedirectToAction("Perfil");
            }

            if (string.IsNullOrEmpty(nuevaPassword) || nuevaPassword.Length < 8)
            {
                TempData["ErrorPassword"] = "La nueva contraseña debe tener al menos 8 caracteres.";
                return RedirectToAction("Perfil");
            }

            if (nuevaPassword != confirmarPassword)
            {
                TempData["ErrorPassword"] = "Las contraseñas nuevas no coinciden.";
                return RedirectToAction("Perfil");
            }

            usuario.PasswordHash = _hasher.HashPassword(usuario, nuevaPassword);
            _context.SaveChanges();

            TempData["MensajePerfil"] = "Tu contraseña fue actualizada correctamente.";
            _auditoria.Registrar(User, "Usuarios", "Cambiar contraseña",
    $"{usuario.Nombre} cambió su contraseña.");
            return RedirectToAction("Perfil");
        }

        private readonly PasswordHasher<Usuario> _hasher = new PasswordHasher<Usuario>();

        private bool VerificarPassword(Usuario usuario, string passwordIngresada, out bool necesitaRehash)
        {
            necesitaRehash = false;

            try
            {
                var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, passwordIngresada);

                if (resultado == PasswordVerificationResult.Success)
                    return true;

                if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    necesitaRehash = true;
                    return true;
                }

                // resultado == Failed: puede ser una contraseña vieja en texto plano
                // que por casualidad "parece" un hash válido en Base64 (como pasó acá)
            }
            catch (FormatException)
            {
                // El valor guardado ni siquiera es Base64 válido -> seguro es texto plano
            }

            // Fallback: comparar directamente como texto plano (cuentas de antes del hashing)
            if (usuario.PasswordHash == passwordIngresada)
            {
                necesitaRehash = true;
                return true;
            }

            return false;
        }
    }
    }
