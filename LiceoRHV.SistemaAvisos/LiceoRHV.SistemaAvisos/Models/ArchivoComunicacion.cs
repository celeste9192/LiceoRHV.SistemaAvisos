using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class ArchivoComunicacion
{
    public int ArchivoId { get; set; }

    public int ComunicacionId { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string Ruta { get; set; } = null!;

    public string? TipoArchivo { get; set; }

    public int? TamanoKb { get; set; }

    public DateTime FechaSubida { get; set; }

    public virtual Comunicacion Comunicacion { get; set; } = null!;
}
