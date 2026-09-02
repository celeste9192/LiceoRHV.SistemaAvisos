using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class RegistroAuditorium
{
    public int AuditoriaId { get; set; }

    public int? UsuarioId { get; set; }

    public string Modulo { get; set; } = null!;

    public string Accion { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
