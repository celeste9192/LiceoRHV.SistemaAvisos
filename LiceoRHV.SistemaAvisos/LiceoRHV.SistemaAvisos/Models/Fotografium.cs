using System;
using System.Collections.Generic;

namespace LiceoRHV.SistemaAvisos.Models;

public partial class Fotografium
{
    public int FotografiaId { get; set; }

    public int EventoId { get; set; }

    public string Archivo { get; set; } = null!;

    public int SubidoPorUsuarioId { get; set; }

    public DateTime FechaSubida { get; set; }

    public virtual Evento Evento { get; set; } = null!;

    public virtual Usuario SubidoPorUsuario { get; set; } = null!;
}
