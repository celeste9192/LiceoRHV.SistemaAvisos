using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Cedula { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string? Telefono { get; set; }

    public int RolId { get; set; }

    public string Estado { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? CodigoRecuperacion { get; set; }
    public DateTime? CodigoRecuperacionExpira { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int? RevisadoPorUsuarioId { get; set; }

    public DateTime? FechaRevision { get; set; }

    public string? MotivoRechazo { get; set; }

    public virtual ICollection<Comunicacion> Comunicacions { get; set; } = new List<Comunicacion>();

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();

    public virtual ICollection<Fotografium> Fotografia { get; set; } = new List<Fotografium>();

    public virtual ICollection<Inscripcion> Inscripcions { get; set; } = new List<Inscripcion>();

    public virtual ICollection<Usuario> InverseRevisadoPorUsuario { get; set; } = new List<Usuario>();

    public virtual ICollection<NormativaInterna> NormativaInternas { get; set; } = new List<NormativaInterna>();

    public virtual ICollection<RegistroAuditorium> RegistroAuditoria { get; set; } = new List<RegistroAuditorium>();

    public virtual Usuario? RevisadoPorUsuario { get; set; }

    public virtual Rol Rol { get; set; } = null!;
}
