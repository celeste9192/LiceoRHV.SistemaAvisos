using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class NormativaInterna
{
    public int NormativaId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string Archivo { get; set; } = null!;

    public DateTime FechaPublicacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public virtual Usuario CreadoPorUsuario { get; set; } = null!;
}
