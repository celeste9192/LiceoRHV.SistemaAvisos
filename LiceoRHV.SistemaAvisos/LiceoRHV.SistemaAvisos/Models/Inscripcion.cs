using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class Inscripcion
{
    public int InscripcionId { get; set; }

    public int EventoId { get; set; }

    public int UsuarioId { get; set; }

    public DateTime FechaInscripcion { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public string? Cedula { get; set; }
}
