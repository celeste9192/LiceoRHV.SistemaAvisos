using System;
using System.Collections.Generic;
using LiceoRHV.SistemaAvisos.Models;
using Microsoft.EntityFrameworkCore;

namespace LiceoRHV.SistemaAvisos.Data;

public partial class LiceoRHVContext : DbContext
{
    public LiceoRHVContext()
    {
    }

    public LiceoRHVContext(DbContextOptions<LiceoRHVContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ArchivoComunicacion> ArchivoComunicacions { get; set; }

    public virtual DbSet<ArchivoEvento> ArchivoEventos { get; set; }

    public virtual DbSet<Categorium> Categoria { get; set; }

    public virtual DbSet<Comunicacion> Comunicacions { get; set; }

    public virtual DbSet<Evento> Eventos { get; set; }

    public virtual DbSet<Fotografium> Fotografia { get; set; }

    public virtual DbSet<Inscripcion> Inscripcions { get; set; }

    public virtual DbSet<NormativaInterna> NormativaInternas { get; set; }

    public virtual DbSet<RegistroAuditorium> RegistroAuditoria { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:LiceoRHVConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArchivoComunicacion>(entity =>
        {
            entity.HasKey(e => e.ArchivoId).HasName("PK__ArchivoC__3D24276AFEEEC2DD");

            entity.ToTable("ArchivoComunicacion");

            entity.Property(e => e.ArchivoId).HasColumnName("ArchivoID");
            entity.Property(e => e.ComunicacionId).HasColumnName("ComunicacionID");
            entity.Property(e => e.FechaSubida)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Ruta)
                .HasMaxLength(400)
                .IsUnicode(false);
            entity.Property(e => e.TamanoKb).HasColumnName("TamanoKB");
            entity.Property(e => e.TipoArchivo)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Comunicacion).WithMany(p => p.ArchivoComunicacions)
                .HasForeignKey(d => d.ComunicacionId)
                .HasConstraintName("FK_ArchivoCom_Comunicacion");
        });

        modelBuilder.Entity<ArchivoEvento>(entity =>
        {
            entity.HasKey(e => e.ArchivoId).HasName("PK__ArchivoE__3D24276A63547892");

            entity.ToTable("ArchivoEvento");

            entity.Property(e => e.ArchivoId).HasColumnName("ArchivoID");
            entity.Property(e => e.EventoId).HasColumnName("EventoID");
            entity.Property(e => e.FechaSubida)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Ruta)
                .HasMaxLength(400)
                .IsUnicode(false);
            entity.Property(e => e.TamanoKb).HasColumnName("TamanoKB");
            entity.Property(e => e.TipoArchivo)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Evento).WithMany(p => p.ArchivoEventos)
                .HasForeignKey(d => d.EventoId)
                .HasConstraintName("FK_ArchivoEve_Evento");
        });

        modelBuilder.Entity<Categorium>(entity =>
        {
            entity.HasKey(e => e.CategoriaId).HasName("PK__Categori__F353C1C56DFB71AC");

            entity.HasIndex(e => e.NombreCategoria, "UQ__Categori__A21FBE9F1C02E97A").IsUnique();

            entity.Property(e => e.CategoriaId).HasColumnName("CategoriaID");
            entity.Property(e => e.NombreCategoria)
                .HasMaxLength(60)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Comunicacion>(entity =>
        {
            entity.HasKey(e => e.ComunicacionId).HasName("PK__Comunica__8A986DF35D9B8E4D");

            entity.ToTable("Comunicacion");

            entity.HasIndex(e => new { e.Estado, e.FechaPublicacion }, "IX_Comunicacion_Estado_Fecha");

            entity.Property(e => e.ComunicacionId).HasColumnName("ComunicacionID");
            entity.Property(e => e.Contenido).IsUnicode(false);
            entity.Property(e => e.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioID");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaPublicacion).HasColumnType("datetime");
            entity.Property(e => e.FechaVencimiento).HasColumnType("datetime");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Titulo)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.Comunicacions)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comunicacion_Usuario");

            entity.HasMany(d => d.Categoria).WithMany(p => p.Comunicacions)
                .UsingEntity<Dictionary<string, object>>(
                    "ComunicacionCategorium",
                    r => r.HasOne<Categorium>().WithMany()
                        .HasForeignKey("CategoriaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ComCat_Categoria"),
                    l => l.HasOne<Comunicacion>().WithMany()
                        .HasForeignKey("ComunicacionId")
                        .HasConstraintName("FK_ComCat_Comunicacion"),
                    j =>
                    {
                        j.HasKey("ComunicacionId", "CategoriaId");
                        j.ToTable("ComunicacionCategoria");
                        j.IndexerProperty<int>("ComunicacionId").HasColumnName("ComunicacionID");
                        j.IndexerProperty<int>("CategoriaId").HasColumnName("CategoriaID");
                    });

            entity.HasMany(d => d.Rols).WithMany(p => p.Comunicacions)
                .UsingEntity<Dictionary<string, object>>(
                    "ComunicacionPublico",
                    r => r.HasOne<Rol>().WithMany()
                        .HasForeignKey("RolId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ComPub_Rol"),
                    l => l.HasOne<Comunicacion>().WithMany()
                        .HasForeignKey("ComunicacionId")
                        .HasConstraintName("FK_ComPub_Comunicacion"),
                    j =>
                    {
                        j.HasKey("ComunicacionId", "RolId");
                        j.ToTable("ComunicacionPublico");
                        j.IndexerProperty<int>("ComunicacionId").HasColumnName("ComunicacionID");
                        j.IndexerProperty<int>("RolId").HasColumnName("RolID");
                    });
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.EventoId).HasName("PK__Evento__1EEB59011BFF05DD");

            entity.ToTable("Evento");

            entity.HasIndex(e => e.FechaEvento, "IX_Evento_Fecha");

            entity.Property(e => e.EventoId).HasColumnName("EventoID");
            entity.Property(e => e.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioID");
            entity.Property(e => e.Descripcion).IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Titulo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Ubicacion)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.Eventos)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Evento_Usuario");

            entity.HasMany(d => d.Categoria).WithMany(p => p.Eventos)
                .UsingEntity<Dictionary<string, object>>(
                    "EventoCategorium",
                    r => r.HasOne<Categorium>().WithMany()
                        .HasForeignKey("CategoriaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EveCat_Categoria"),
                    l => l.HasOne<Evento>().WithMany()
                        .HasForeignKey("EventoId")
                        .HasConstraintName("FK_EveCat_Evento"),
                    j =>
                    {
                        j.HasKey("EventoId", "CategoriaId");
                        j.ToTable("EventoCategoria");
                        j.IndexerProperty<int>("EventoId").HasColumnName("EventoID");
                        j.IndexerProperty<int>("CategoriaId").HasColumnName("CategoriaID");
                    });

            entity.HasMany(d => d.Rols).WithMany(p => p.Eventos)
                .UsingEntity<Dictionary<string, object>>(
                    "EventoPublico",
                    r => r.HasOne<Rol>().WithMany()
                        .HasForeignKey("RolId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_EvePub_Rol"),
                    l => l.HasOne<Evento>().WithMany()
                        .HasForeignKey("EventoId")
                        .HasConstraintName("FK_EvePub_Evento"),
                    j =>
                    {
                        j.HasKey("EventoId", "RolId");
                        j.ToTable("EventoPublico");
                        j.IndexerProperty<int>("EventoId").HasColumnName("EventoID");
                        j.IndexerProperty<int>("RolId").HasColumnName("RolID");
                    });
        });

        modelBuilder.Entity<Fotografium>(entity =>
        {
            entity.HasKey(e => e.FotografiaId).HasName("PK__Fotograf__D2BA5264B3A72AB1");

            entity.Property(e => e.FotografiaId).HasColumnName("FotografiaID");
            entity.Property(e => e.Archivo)
                .HasMaxLength(400)
                .IsUnicode(false);
            entity.Property(e => e.EventoId).HasColumnName("EventoID");
            entity.Property(e => e.FechaSubida)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SubidoPorUsuarioId).HasColumnName("SubidoPorUsuarioID");

            entity.HasOne(d => d.Evento).WithMany(p => p.Fotografia)
                .HasForeignKey(d => d.EventoId)
                .HasConstraintName("FK_Fotografia_Evento");

            entity.HasOne(d => d.SubidoPorUsuario).WithMany(p => p.Fotografia)
                .HasForeignKey(d => d.SubidoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fotografia_Usuario");
        });

        modelBuilder.Entity<Inscripcion>(entity =>
        {
            entity.HasKey(e => e.InscripcionId).HasName("PK__Inscripc__168316990DF569B6");

            entity.ToTable("Inscripcion");

            entity.HasIndex(e => new { e.EventoId, e.UsuarioId }, "UQ_Inscripcion_EventoUsuario").IsUnique();

            entity.Property(e => e.InscripcionId).HasColumnName("InscripcionID");
            entity.Property(e => e.EventoId).HasColumnName("EventoID");
            entity.Property(e => e.FechaInscripcion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");

            entity.HasOne(d => d.Evento).WithMany(p => p.Inscripcions)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inscripcion_Evento");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Inscripcions)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inscripcion_Usuario");
        });

        modelBuilder.Entity<NormativaInterna>(entity =>
        {
            entity.HasKey(e => e.NormativaId).HasName("PK__Normativ__24A4F79BAA94AE79");

            entity.ToTable("NormativaInterna");

            entity.Property(e => e.NormativaId).HasColumnName("NormativaID");
            entity.Property(e => e.Archivo)
                .HasMaxLength(400)
                .IsUnicode(false);
            entity.Property(e => e.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioID");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaActualizacion).HasColumnType("datetime");
            entity.Property(e => e.FechaPublicacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Titulo)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.NormativaInternas)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Normativa_Usuario");
        });

        modelBuilder.Entity<RegistroAuditorium>(entity =>
        {
            entity.HasKey(e => e.AuditoriaId).HasName("PK__Registro__095694E31F1D7EC0");

            entity.HasIndex(e => e.FechaHora, "IX_Auditoria_Fecha");

            entity.HasIndex(e => e.UsuarioId, "IX_Auditoria_Usuario");

            entity.Property(e => e.AuditoriaId).HasColumnName("AuditoriaID");
            entity.Property(e => e.Accion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Modulo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");

            entity.HasOne(d => d.Usuario).WithMany(p => p.RegistroAuditoria)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_Auditoria_Usuario");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__Rol__F92302D12BC446A8");

            entity.ToTable("Rol");

            entity.HasIndex(e => e.NombreRol, "UQ__Rol__4F0B537FA5B09AD1").IsUnique();

            entity.Property(e => e.RolId).HasColumnName("RolID");
            entity.Property(e => e.NombreRol)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("PK__Usuario__2B3DE7981AB26C48");

            entity.ToTable("Usuario");

            entity.HasIndex(e => e.Estado, "IX_Usuario_Estado");

            entity.HasIndex(e => e.Correo, "UQ__Usuario__60695A19D802C1C1").IsUnique();

            entity.HasIndex(e => e.Cedula, "UQ__Usuario__B4ADFE383E1195F6").IsUnique();

            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(e => e.Cedula)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaRevision).HasColumnType("datetime");
            entity.Property(e => e.MotivoRechazo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RevisadoPorUsuarioId).HasColumnName("RevisadoPorUsuarioID");
            entity.Property(e => e.RolId).HasColumnName("RolID");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.RevisadoPorUsuario).WithMany(p => p.InverseRevisadoPorUsuario)
                .HasForeignKey(d => d.RevisadoPorUsuarioId)
                .HasConstraintName("FK_Usuario_RevisadoPor");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Rol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
