using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class Categorium
{
    public int CategoriaId { get; set; }

    public string NombreCategoria { get; set; } = null!;

    public virtual ICollection<Comunicacion> Comunicacions { get; set; } = new List<Comunicacion>();

    public virtual ICollection<Evento> Eventos { get; set; } = new List<Evento>();
}
