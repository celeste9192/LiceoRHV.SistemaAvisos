using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class Rol
{
    public int RolId { get; set; }

    public string NombreRol { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

    public virtual ICollection<Comunicacion> Comunicacions { get; set; } = new List<Comunicacion>();

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}
