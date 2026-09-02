using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class Comunicacion
{
    public int ComunicacionId { get; set; }

    public string Titulo { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public bool Destacada { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<ArchivoComunicacion> ArchivoComunicacions { get; set; } = new List<ArchivoComunicacion>();

    public virtual Usuario CreadoPorUsuario { get; set; } = null!;

    public virtual ICollection<Categorium> Categoria { get; set; } = new List<Categorium>();

    public virtual ICollection<Rol> Rols { get; set; } = new List<Rol>();
}
