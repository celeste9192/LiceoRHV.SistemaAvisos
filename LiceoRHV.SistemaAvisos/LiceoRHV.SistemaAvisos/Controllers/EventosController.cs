using System.Linq;
using System.Reflection.Metadata;
using System.Security.Claims;
using LiceoRHV.SistemaAvisos.Data;
using LiceoRHV.SistemaAvisos.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LiceoRHV.SistemaAvisos.Services;

namespace LiceoRHV.SistemaAvisos.Controllers
{
    public class EventosController : Controller
    {
        private readonly LiceoRHVContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditoriaService _auditoria;

        public EventosController(LiceoRHVContext context, IWebHostEnvironment env, AuditoriaService auditoria)
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

        private void ActualizarEstadosEventos()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var paraFinalizar = _context.Eventos
                .Where(e => e.Estado == "Publicado" && e.FechaEvento < hoy)
                .ToList();
            foreach (var e in paraFinalizar) e.Estado = "Finalizado";
            if (paraFinalizar.Count > 0) _context.SaveChanges();
        }

        public IActionResult Index(string? estado, int? rolId, int? categoriaId, string? titulo)
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");

            ActualizarEstadosEventos();

            var query = _context.Eventos
                .Include(e => e.Rols)
                .Include(e => e.Categoria)
                .Include(e => e.Inscripcions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(e => e.Estado == estado);

            if (rolId.HasValue)
                query = query.Where(e => e.Rols.Any(r => r.RolId == rolId.Value));

            if (categoriaId.HasValue)
                query = query.Where(e => e.Categoria.Any(c => c.CategoriaId == categoriaId.Value));

            if (!string.IsNullOrWhiteSpace(titulo))
                query = query.Where(e => e.Titulo.Contains(titulo));

            var eventos = query.OrderByDescending(e => e.FechaEvento).ToList();

            ViewBag.RolesFiltro = new SelectList(_context.Rols, "RolId", "NombreRol", rolId);
            ViewBag.CategoriasFiltro = new SelectList(_context.Categoria, "CategoriaId", "NombreCategoria", categoriaId);
            ViewBag.FiltroEstado = estado ?? "";
            ViewBag.FiltroTitulo = titulo ?? "";

            return View(eventos);
        }

        public IActionResult Create()
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");
            ViewBag.Roles = _context.Rols.ToList();
            ViewBag.Categorias = _context.Categoria.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create(
            string Titulo, string? Descripcion, DateOnly FechaEvento, TimeOnly HoraEvento,
            string? Ubicacion, bool RequiereInscripcion, int? CupoMaximo,
            List<int> rolesSeleccionados, List<int>? categoriasSeleccionadas,
            List<IFormFile>? archivos)
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");

            ViewBag.Roles = _context.Rols.ToList();
            ViewBag.Categorias = _context.Categoria.ToList();

            if (string.IsNullOrWhiteSpace(Titulo))
            {
                ViewBag.ErrorEvento = "El título es obligatorio.";
                return View();
            }

            if (rolesSeleccionados == null || rolesSeleccionados.Count == 0)
            {
                ViewBag.ErrorEvento = "Seleccioná al menos un grupo destinatario.";
                return View();
            }

            if (RequiereInscripcion && CupoMaximo.HasValue && CupoMaximo <= 0)
            {
                ViewBag.ErrorEvento = "El cupo máximo debe ser mayor a cero.";
                return View();
            }

            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId = usuarioIdClaim != null ? int.Parse(usuarioIdClaim) : 0;

            var evento = new Evento
            {
                Titulo = Titulo,
                Descripcion = Descripcion,
                FechaEvento = FechaEvento,
                HoraEvento = HoraEvento,
                Ubicacion = Ubicacion,
                Estado = "Publicado",
                RequiereInscripcion = RequiereInscripcion,
                CupoMaximo = RequiereInscripcion ? CupoMaximo : null,
                CreadoPorUsuarioId = usuarioId,
                FechaCreacion = DateTime.Now
            };

            var rolesElegidos = _context.Rols.Where(r => rolesSeleccionados.Contains(r.RolId)).ToList();
            foreach (var rol in rolesElegidos) evento.Rols.Add(rol);

            if (categoriasSeleccionadas != null && categoriasSeleccionadas.Count > 0)
            {
                var categoriasElegidas = _context.Categoria.Where(c => categoriasSeleccionadas.Contains(c.CategoriaId)).ToList();
                foreach (var cat in categoriasElegidas) evento.Categoria.Add(cat);
            }

            _context.Eventos.Add(evento);
            _context.SaveChanges();

            if (archivos != null && archivos.Count > 0)
            {
                var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
                var carpetaDestino = Path.Combine(_env.WebRootPath, "uploads", "eventos");
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

                    _context.ArchivoEventos.Add(new ArchivoEvento
                    {
                        EventoId = evento.EventoId,
                        NombreArchivo = archivo.FileName,
                        Ruta = "/uploads/eventos/" + nombreUnico,
                        TipoArchivo = archivo.ContentType,
                        TamanoKb = (int)(archivo.Length / 1024),
                        FechaSubida = DateTime.Now
                    });
                }
                _context.SaveChanges();
            }

            TempData["MensajeEvento"] = "Evento publicado correctamente.";
            _auditoria.Registrar(User, "Eventos", "Crear",
    $"Se creó el evento '{evento.Titulo}' para el {evento.FechaEvento:dd/MM/yyyy}.");

            return RedirectToAction("Index");
        }

        public IActionResult Detalle(int id)
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");

            var evento = _context.Eventos
                .Include(e => e.Rols)
                .Include(e => e.Categoria)
                .Include(e => e.ArchivoEventos)
                .Include(e => e.Inscripcions)
                .Include(e => e.CreadoPorUsuario)
                .FirstOrDefault(e => e.EventoId == id);

            if (evento == null) return NotFound();

            ViewBag.Roles = _context.Rols.ToList();
            ViewBag.Categorias = _context.Categoria.ToList();

            return PartialView("_DetalleEvento", evento);
        }
        [HttpPost]
        public IActionResult EditarEvento(
            int EventoId, string Titulo, string? Descripcion, DateOnly FechaEvento, TimeOnly HoraEvento,
            string? Ubicacion, bool RequiereInscripcion, int? CupoMaximo,
            List<int> rolesSeleccionados, List<int>? categoriasSeleccionadas)
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");

            var evento = _context.Eventos
                .Include(e => e.Rols)
                .Include(e => e.Categoria)
                .FirstOrDefault(e => e.EventoId == EventoId);

            if (evento == null) return NotFound();

            evento.Titulo = Titulo;
            evento.Descripcion = Descripcion;
            evento.FechaEvento = FechaEvento;
            evento.HoraEvento = HoraEvento;
            evento.Ubicacion = Ubicacion;
            evento.RequiereInscripcion = RequiereInscripcion;
            evento.CupoMaximo = RequiereInscripcion ? CupoMaximo : null;

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            evento.Estado = FechaEvento < hoy ? "Finalizado" : "Publicado";

            evento.Rols.Clear();
            if (rolesSeleccionados != null)
            {
                var rolesElegidos = _context.Rols.Where(r => rolesSeleccionados.Contains(r.RolId)).ToList();
                foreach (var rol in rolesElegidos) evento.Rols.Add(rol);
            }

            evento.Categoria.Clear();
            if (categoriasSeleccionadas != null)
            {
                var categoriasElegidas = _context.Categoria.Where(c => categoriasSeleccionadas.Contains(c.CategoriaId)).ToList();
                foreach (var cat in categoriasElegidas) evento.Categoria.Add(cat);
            }

            _context.SaveChanges();

            TempData["MensajeEvento"] = "Evento actualizado correctamente.";
            _auditoria.Registrar(User, "Eventos", "Editar",
    $"Se editó el evento '{evento.Titulo}'.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");

            var evento = _context.Eventos
                .Include(e => e.ArchivoEventos)
                .Include(e => e.Inscripcions)
                .FirstOrDefault(e => e.EventoId == id);

            if (evento == null) return NotFound();

            foreach (var archivo in evento.ArchivoEventos)
            {
                var rutaRelativa = archivo.Ruta.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);
                if (System.IO.File.Exists(rutaFisica)) System.IO.File.Delete(rutaFisica);
            }

            _context.Inscripcions.RemoveRange(evento.Inscripcions);
            _context.Eventos.Remove(evento);
            _context.SaveChanges();

            TempData["MensajeEvento"] = "Evento eliminado correctamente.";
            _auditoria.Registrar(User, "Eventos", "Eliminar",
    $"Se eliminó el evento '{evento.Titulo}'.");
            return RedirectToAction("Index");
        }

        // POST: Eventos/Inscribirse (llamado desde el Home)
        [HttpPost]
        public IActionResult Inscribirse(int id)
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId = usuarioIdClaim != null ? int.Parse(usuarioIdClaim) : 0;

            var usuario = _context.Usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId);
            if (usuario == null) return RedirectToAction("Index", "Home");

            var evento = _context.Eventos
                .Include(e => e.Inscripcions)
                .FirstOrDefault(e => e.EventoId == id);

            if (evento == null) return RedirectToAction("Index", "Home");

            bool yaInscrito = evento.Inscripcions.Any(i => i.UsuarioId == usuarioId);
            if (yaInscrito)
            {
                TempData["ErrorEvento" + id] = "Ya estás inscrito en este evento.";
                return RedirectToAction("Index", "Home");
            }

            if (evento.CupoMaximo.HasValue && evento.Inscripcions.Count >= evento.CupoMaximo.Value)
            {
                TempData["ErrorEvento" + id] = "Ya no hay cupos disponibles para este evento.";
                return RedirectToAction("Index", "Home");
            }

            _context.Inscripcions.Add(new Inscripcion
            {
                EventoId = id,
                UsuarioId = usuarioId,
                Cedula = usuario.Cedula,
                FechaInscripcion = DateTime.Now
            });
            _context.SaveChanges();

            TempData["MensajeEvento"] = "¡Listo! Quedaste inscrito en el evento.";
            _auditoria.Registrar(User, "Eventos", "Inscripción",
    $"{usuario.Nombre} se inscribió en el evento '{evento.Titulo}'.");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult CancelarInscripcion(int id)
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId = usuarioIdClaim != null ? int.Parse(usuarioIdClaim) : 0;

            var inscripcion = _context.Inscripcions
                .FirstOrDefault(i => i.EventoId == id && i.UsuarioId == usuarioId);

            if (inscripcion != null)
            {
                _context.Inscripcions.Remove(inscripcion);
                _context.SaveChanges();
                TempData["MensajeEvento"] = "Tu inscripción fue cancelada.";
            }
            _auditoria.Registrar(User, "Eventos", "Cancelar inscripción",
    $"Se canceló una inscripción en el evento con ID {id}.");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ListaInscritos(int id)
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");

            var evento = _context.Eventos
                .Include(e => e.Inscripcions)
                    .ThenInclude(i => i.Usuario)
                .FirstOrDefault(e => e.EventoId == id);

            if (evento == null) return NotFound();

            return View(evento);
        }

        public IActionResult DescargarInscritosPdf(int id)
        {
            if (!EsGestion()) return RedirectToAction("Index", "Home");

            var evento = _context.Eventos
                .Include(e => e.Inscripcions)
                    .ThenInclude(i => i.Usuario)
                .FirstOrDefault(e => e.EventoId == id);

            if (evento == null) return NotFound();

            QuestPDF.Settings.License = LicenseType.Community;

            var documento = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text("Lista de Inscritos").FontSize(18).Bold();

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(10).Text(evento.Titulo).FontSize(15).Bold();

                        if (!string.IsNullOrEmpty(evento.Descripcion))
                        {
                            col.Item().PaddingTop(4).Text(evento.Descripcion).FontSize(10).Italic();
                        }

                        col.Item().PaddingTop(10).Text($"Fecha del evento: {evento.FechaEvento:dd/MM/yyyy} - {evento.HoraEvento:HH:mm}");
                        col.Item().Text($"Total de inscritos: {evento.Inscripcions.Count}" +
                            (evento.CupoMaximo != null ? $" / {evento.CupoMaximo}" : ""));

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Nombre").Bold();
                                header.Cell().Text("Cédula").Bold();
                                header.Cell().Text("Correo").Bold();
                                header.Cell().Text("Fecha de inscripción").Bold();
                            });

                            foreach (var inscripcion in evento.Inscripcions.OrderBy(i => i.FechaInscripcion))
                            {
                                table.Cell().Text(inscripcion.Usuario.Nombre);
                                table.Cell().Text(inscripcion.Cedula ?? "—");
                                table.Cell().Text(inscripcion.Usuario.Correo);
                                table.Cell().Text(inscripcion.FechaInscripcion.ToString("dd/MM/yyyy HH:mm"));
                            }

                           
                        });
                    });
                });
            });

            var bytes = documento.GeneratePdf();
            return File(bytes, "application/pdf", $"inscritos_{evento.Titulo}.pdf");
        }
    }
}