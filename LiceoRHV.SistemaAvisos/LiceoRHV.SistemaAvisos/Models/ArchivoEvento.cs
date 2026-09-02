using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class ArchivoEvento
{
    public int ArchivoId { get; set; }

    public int EventoId { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string Ruta { get; set; } = null!;

    public string? TipoArchivo { get; set; }

    public int? TamanoKb { get; set; }

    public DateTime FechaSubida { get; set; }

    public virtual Evento Evento { get; set; } = null!;
}
