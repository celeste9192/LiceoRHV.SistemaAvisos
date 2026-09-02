using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class Evento
{
    public int EventoId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateOnly FechaEvento { get; set; }

    public TimeOnly HoraEvento { get; set; }

    public string? Ubicacion { get; set; }

    public string Estado { get; set; } = null!;

    public bool RequiereInscripcion { get; set; }

    public int? CupoMaximo { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<ArchivoEvento> ArchivoEventos { get; set; } = new List<ArchivoEvento>();

    public virtual Usuario CreadoPorUsuario { get; set; } = null!;

    public virtual ICollection<Fotografium> Fotografia { get; set; } = new List<Fotografium>();

    public virtual ICollection<Inscripcion> Inscripcions { get; set; } = new List<Inscripcion>();

    public virtual ICollection<Categorium> Categoria { get; set; } = new List<Categorium>();

    public virtual ICollection<Rol> Rols { get; set; } = new List<Rol>();
}
