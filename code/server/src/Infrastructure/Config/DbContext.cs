using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Config;

public class ApplicationDbContext : DbContext
{
    private const string EstadoEquipoCol = "estado_equipo";
    private const string DescripcionCol = "descripcion";
    private const string NombreCol = "nombre";
    private const string EstadoEliminadoCol = "estado_eliminado";
    private const string IdEquipoCol = "id_equipo";

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Carrera> Carreras { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<GrupoEquipo> GruposEquipos { get; set; }
    public DbSet<Mueble> Muebles { get; set; }
    public DbSet<Gavetero> Gaveteros { get; set; }
    public DbSet<Equipo> Equipos { get; set; }
    public DbSet<Accesorio> Accesorios { get; set; }
    public DbSet<Componente> Componentes { get; set; }
    public DbSet<EmpresaMantenimiento> EmpresasMantenimiento { get; set; }
    public DbSet<Mantenimiento> Mantenimientos { get; set; }
    public DbSet<DetalleMantenimiento> DetallesMantenimientos { get; set; }
    public DbSet<Prestamo> Prestamos { get; set; }
    public DbSet<DetallePrestamo> DetallesPrestamos { get; set; }
    public DbSet<Contrato> Contratos { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }
    public DbSet<AvisoDisponibilidad> AvisosDisponibilidad { get; set; }
    public DbSet<ComentarioEquipo> ComentariosEquipos { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresEnum<EstadoPrestamo>("estado_prestamo");
        modelBuilder.HasPostgresEnum<TipoUsuario>("tipo_usuario");
        modelBuilder.HasPostgresEnum<EstadoEquipo>(EstadoEquipoCol);

        var dateTimeConverter =
            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<
                DateTime,
                DateTime
            >(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entityType.GetProperties())
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(dateTimeConverter);

        modelBuilder.Entity<Carrera>(entity =>
        {
            entity.ToTable("carreras");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_carrera");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(NombreCol);
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_categoria");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(NombreCol);
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<EmpresaMantenimiento>(entity =>
        {
            entity.ToTable("empresas_mantenimiento");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_empresa_mantenimiento");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(NombreCol);
            entity.Property(e => e.Nit).HasMaxLength(20).HasColumnName("nit");
            entity.Property(e => e.Direccion).HasMaxLength(512).HasColumnName("direccion");
            entity.Property(e => e.Telefono).HasMaxLength(64).HasColumnName("telefono");
            entity
                .Property(e => e.NombreResponsable)
                .HasMaxLength(64)
                .HasColumnName("nombre_responsable");
            entity
                .Property(e => e.ApellidoResponsable)
                .HasMaxLength(64)
                .HasColumnName("apellido_responsable");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<GrupoEquipo>(entity =>
        {
            entity.ToTable("grupos_equipos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_grupo_equipo");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(256).HasColumnName(NombreCol);
            entity.Property(e => e.Modelo).IsRequired().HasMaxLength(512).HasColumnName("modelo");
            entity.Property(e => e.Marca).IsRequired().HasMaxLength(256).HasColumnName("marca");
            entity
                .Property(e => e.Descripcion)
                .IsRequired()
                .HasMaxLength(2048)
                .HasColumnName(DescripcionCol);
            entity.Property(e => e.UrlDataSheet).HasMaxLength(2048).HasColumnName("url_data_sheet");
            entity
                .Property(e => e.UrlImagen)
                .IsRequired()
                .HasMaxLength(2048)
                .HasColumnName("url_imagen");
            entity
                .Property(e => e.CostoPromedio)
                .HasPrecision(10, 2)
                .HasColumnName("costo_promedio");
            entity.Property(e => e.Cantidad).HasDefaultValue(0).HasColumnName("cantidad");
            entity
                .Property(e => e.TiempoMaximoPrestamoDias)
                .HasDefaultValue(7)
                .HasColumnName("tiempo_max_prestamo_dias");
            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity
                .HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(e => e.IdCategoria)
                .IsRequired();
            entity.HasIndex(e => new
            {
                e.IdCategoria,
                e.Nombre,
                e.Modelo,
                e.Marca,
                e.EstadoEliminado,
            });
            entity
                .HasIndex(e => new
                {
                    e.Nombre,
                    e.Modelo,
                    e.Marca,
                })
                .IsUnique();
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Mueble>(entity =>
        {
            entity.ToTable("muebles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_mueble");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(NombreCol);
            entity.Property(e => e.Tipo).HasMaxLength(255).HasColumnName("tipo");
            entity.Property(e => e.Ubicacion).HasMaxLength(255).HasColumnName("ubicacion");
            entity
                .Property(e => e.NumeroGaveteros)
                .HasDefaultValue(0)
                .HasColumnName("numero_gaveteros");
            entity.Property(e => e.Longitud).HasColumnName("longitud");
            entity.Property(e => e.Profundidad).HasColumnName("profundidad");
            entity.Property(e => e.Altura).HasColumnName("altura");
            entity.Property(e => e.Costo).HasColumnName("costo");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Gavetero>(entity =>
        {
            entity.ToTable("gaveteros");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_gavetero");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(NombreCol);
            entity.Property(e => e.Tipo).HasMaxLength(255).HasColumnName("tipo");
            entity.Property(e => e.IdMueble).HasColumnName("id_mueble");
            entity.Property(e => e.Longitud).HasColumnName("longitud");
            entity.Property(e => e.Profundidad).HasColumnName("profundidad");
            entity.Property(e => e.Altura).HasColumnName("altura");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasOne(e => e.Mueble).WithMany().HasForeignKey(e => e.IdMueble).IsRequired();
            entity.HasIndex(e => new
            {
                e.Nombre,
                e.IdMueble,
                e.EstadoEliminado,
            });
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.ToTable("equipos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName(IdEquipoCol);
            entity.Property(e => e.CodigoImt).IsRequired().HasColumnName("codigo_imt");
            entity.Property(e => e.CodigoUcb).HasMaxLength(256).HasColumnName("codigo_ucb");
            entity.Property(e => e.NumeroSerial).HasMaxLength(255).HasColumnName("numero_serial");
            entity.Property(e => e.Ubicacion).HasMaxLength(255).HasColumnName("ubicacion");
            entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DescripcionCol);
            entity.Property(e => e.CostoReferencia).HasColumnName("costo_referencia");
            entity.Property(e => e.Procedencia).HasMaxLength(255).HasColumnName("procedencia");
            entity
                .Property(e => e.FechaIngresoEquipo)
                .HasDefaultValue(DateOnly.FromDateTime(DateTime.UtcNow))
                .HasColumnName("fecha_ingreso_equipo");
            entity
                .Property(e => e.EstadoEquipo)
                .HasColumnType(EstadoEquipoCol)
                .HasDefaultValue(EstadoEquipo.Operativo)
                .HasColumnName(EstadoEquipoCol);
            entity.Property(e => e.IdGrupoEquipo).HasColumnName("id_grupo_equipo");
            entity.Property(e => e.IdGavetero).HasColumnName("id_gavetero");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity
                .HasOne(e => e.GrupoEquipo)
                .WithMany()
                .HasForeignKey(e => e.IdGrupoEquipo)
                .IsRequired();
            entity.HasOne(e => e.Gavetero).WithMany().HasForeignKey(e => e.IdGavetero);
            entity.HasIndex(e => new
            {
                e.IdGrupoEquipo,
                e.CodigoImt,
                e.EstadoEliminado,
            });
            entity.HasIndex(e => e.CodigoImt).IsUnique();
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Accesorio>(entity =>
        {
            entity.ToTable("accesorios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_accesorio");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(NombreCol);
            entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DescripcionCol);
            entity.Property(e => e.Modelo).IsRequired().HasMaxLength(255).HasColumnName("modelo");
            entity.Property(e => e.UrlDataSheet).HasMaxLength(2048).HasColumnName("url_data_sheet");
            entity.Property(e => e.Tipo).HasMaxLength(255).HasColumnName("tipo");
            entity.Property(e => e.Precio).HasColumnName("precio");
            entity.Property(e => e.IdEquipo).HasColumnName(IdEquipoCol);
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasOne<Equipo>().WithMany().HasForeignKey(e => e.IdEquipo).IsRequired();
            entity.HasIndex(e => new
            {
                e.Nombre,
                e.IdEquipo,
                e.EstadoEliminado,
            });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Componente>(entity =>
        {
            entity.ToTable("componentes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_componente");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(255).HasColumnName(NombreCol);
            entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DescripcionCol);
            entity.Property(e => e.Modelo).IsRequired().HasMaxLength(255).HasColumnName("modelo");
            entity.Property(e => e.UrlDataSheet).HasMaxLength(2048).HasColumnName("url_data_sheet");
            entity.Property(e => e.Tipo).HasMaxLength(255).HasColumnName("tipo");
            entity.Property(e => e.PrecioReferencia).HasColumnName("precio_referencia");
            entity.Property(e => e.IdEquipo).HasColumnName(IdEquipoCol);
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasOne<Equipo>().WithMany().HasForeignKey(e => e.IdEquipo).IsRequired();
            entity.HasIndex(e => new
            {
                e.Nombre,
                e.IdEquipo,
                e.EstadoEliminado,
            });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Mantenimiento>(entity =>
        {
            entity.ToTable("mantenimientos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_mantenimiento");
            entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DescripcionCol);
            entity.Property(e => e.Costo).HasColumnName("costo");
            entity
                .Property(e => e.FechaMantenimiento)
                .IsRequired()
                .HasColumnName("fecha_mantenimiento");
            entity
                .Property(e => e.FechaFinalMantenimiento)
                .IsRequired()
                .HasColumnName("fecha_final_mantenimiento");
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity
                .HasOne<EmpresaMantenimiento>()
                .WithMany()
                .HasForeignKey(e => e.IdEmpresa)
                .IsRequired();
            entity.HasIndex(e => new
            {
                e.FechaMantenimiento,
                e.FechaFinalMantenimiento,
                e.IdEmpresa,
                e.EstadoEliminado,
            });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<DetalleMantenimiento>(entity =>
        {
            entity.ToTable("detalles_mantenimientos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_detalle_mantenimiento");
            entity
                .Property(e => e.TipoMantenimiento)
                .HasMaxLength(256)
                .HasColumnName("tipo_mantenimiento");
            entity.Property(e => e.Descripcion).HasMaxLength(2048).HasColumnName(DescripcionCol);
            entity.Property(e => e.IdMantenimiento).HasColumnName("id_mantenimiento");
            entity.Property(e => e.IdEquipo).HasColumnName(IdEquipoCol);
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity
                .HasOne<Mantenimiento>()
                .WithMany()
                .HasForeignKey(e => e.IdMantenimiento)
                .IsRequired();
            entity.HasOne<Equipo>().WithMany().HasForeignKey(e => e.IdEquipo).IsRequired();
            entity.HasIndex(e => new { e.IdMantenimiento, e.EstadoEliminado });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(e => e.Carnet);
            entity.Ignore(e => e.Id);
            entity.Property(e => e.Carnet).HasMaxLength(20).HasColumnName("carnet");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(64).HasColumnName(NombreCol);
            entity
                .Property(e => e.ApellidoPaterno)
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnName("apellido_paterno");
            entity
                .Property(e => e.ApellidoMaterno)
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnName("apellido_materno");
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255).HasColumnName("email");
            entity
                .Property(e => e.Contrasena)
                .IsRequired()
                .HasMaxLength(72)
                .HasColumnName("contrasena");
            entity
                .Property(e => e.Telefono)
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnName("telefono");
            entity
                .Property(e => e.TelefonoReferencia)
                .HasMaxLength(32)
                .HasColumnName("telefono_referencia");
            entity
                .Property(e => e.NombreReferencia)
                .HasMaxLength(32)
                .HasColumnName("nombre_referencia");
            entity
                .Property(e => e.EmailReferencia)
                .HasMaxLength(255)
                .HasColumnName("email_referencia");
            entity
                .Property(e => e.Rol)
                .HasColumnType("tipo_usuario")
                .HasDefaultValue(TipoUsuario.Estudiante)
                .HasColumnName("rol");
            entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
            entity.Property(e => e.Bloqueado).HasColumnName("bloqueado");
            entity.Property(e => e.MotivoBloqueo).HasColumnName("motivo_bloqueo");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.Property(e => e.ImagenFrenteCarnet).HasColumnName("imagen_frente_carnet");
            entity.Property(e => e.ImagenAtrasCarnet).HasColumnName("imagen_atras_carnet");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiry).HasColumnName("refresh_token_expiry");
            entity.HasOne<Carrera>().WithMany().HasForeignKey(e => e.IdCarrera).IsRequired();
            entity.HasIndex(e => new { e.Nombre, e.EstadoEliminado });
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.RefreshToken);
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Prestamo>(entity =>
        {
            entity.ToTable("prestamos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_prestamo");
            entity
                .Property(e => e.FechaSolicitud)
                .HasDefaultValueSql("now() AT TIME ZONE 'America/La_Paz'")
                .HasColumnName("fecha_solicitud");
            entity.Property(e => e.FechaPrestamo).HasColumnName("fecha_prestamo");
            entity
                .Property(e => e.FechaPrestamoEsperada)
                .IsRequired()
                .HasColumnName("fecha_prestamo_esperada");
            entity.Property(e => e.FechaDevolucion).HasColumnName("fecha_devolucion");
            entity
                .Property(e => e.FechaDevolucionEsperada)
                .IsRequired()
                .HasColumnName("fecha_devolucion_esperada");
            entity.Property(e => e.Observacion).HasMaxLength(1024).HasColumnName("observacion");
            entity
                .Property(e => e.EstadoPrestamo)
                .HasColumnType("estado_prestamo")
                .HasDefaultValue(EstadoPrestamo.Pendiente)
                .HasColumnName("estado_prestamo");
            entity.Property(e => e.IdContrato).HasColumnName("id_contrato");
            entity.Property(e => e.Carnet).HasMaxLength(20).HasColumnName("carnet");
            entity
                .Property(e => e.RecordatorioEnviado)
                .HasColumnName("recordatorio_enviado")
                .HasDefaultValue(false);
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.Carnet).IsRequired();
            entity.HasOne<Contrato>().WithMany().HasForeignKey(e => e.IdContrato);
            entity.HasIndex(e => e.IdContrato);
            entity.HasIndex(e => new
            {
                e.FechaPrestamoEsperada,
                e.FechaDevolucionEsperada,
                e.Carnet,
                e.EstadoEliminado,
            });
            entity.HasIndex(e => new
            {
                e.Carnet,
                e.EstadoPrestamo,
                e.EstadoEliminado,
            });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Contrato>(entity =>
        {
            entity.ToTable("contratos");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.EstadoEliminado);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContratoHtml).HasColumnType("text").HasColumnName("contrato");
        });

        modelBuilder.Entity<DetallePrestamo>(entity =>
        {
            entity.ToTable("detalles_prestamos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_detalle_prestamo");
            entity.Property(e => e.IdPrestamo).HasColumnName("id_prestamo");
            entity.Property(e => e.IdEquipo).HasColumnName(IdEquipoCol);
            entity.Property(e => e.IdGrupoEquipo).HasColumnName("id_grupo_equipo");
            entity
                .Property(e => e.EstadoEquipoRetorno)
                .HasColumnType(EstadoEquipoCol)
                .HasColumnName("estado_equipo_retorno");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasOne<Prestamo>().WithMany().HasForeignKey(e => e.IdPrestamo).IsRequired();
            entity.HasOne<Equipo>().WithMany().HasForeignKey(e => e.IdEquipo).IsRequired(false);
            entity
                .HasOne<GrupoEquipo>()
                .WithMany()
                .HasForeignKey(e => e.IdGrupoEquipo)
                .IsRequired();
            entity.HasIndex(e => new { e.IdPrestamo, e.EstadoEliminado });
            entity.HasIndex(e => e.IdEquipo);
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdminCarnet).HasColumnName("admin_carnet").HasMaxLength(20);
            entity.Property(e => e.AdminNombre).HasColumnName("admin_nombre");
            entity.Property(e => e.Accion).HasColumnName("accion").HasMaxLength(50);
            entity.Property(e => e.Entidad).HasColumnName("entidad").HasMaxLength(100);
            entity.Property(e => e.EntidadId).HasColumnName("entidad_id");
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasIndex(e => new
            {
                e.AdminCarnet,
                e.Timestamp,
                e.EstadoEliminado,
            });
            entity.HasIndex(e => new
            {
                e.Entidad,
                e.EntidadId,
                e.EstadoEliminado,
            });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<ComentarioEquipo>(entity =>
        {
            entity.ToTable("comentarios_equipos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_comentario_equipo");
            entity.Property(e => e.IdGrupoEquipo).HasColumnName("id_grupo_equipo");
            entity.Property(e => e.IdComentarioPadre).HasColumnName("id_comentario_padre");
            entity.Property(e => e.CarnetUsuario).HasColumnName("carnet_usuario").HasMaxLength(20);
            entity.Property(e => e.Contenido).HasColumnName("contenido").HasMaxLength(1024);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.Likes).HasColumnName("likes").HasDefaultValue(0);
            entity
                .Property(e => e.LikedBy)
                .HasColumnName("liked_by")
                .HasColumnType("text")
                .HasDefaultValue(string.Empty);
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity
                .HasOne(e => e.GrupoEquipo)
                .WithMany()
                .HasForeignKey(e => e.IdGrupoEquipo)
                .IsRequired();
            entity
                .HasOne(e => e.ComentarioPadre)
                .WithMany()
                .HasForeignKey(e => e.IdComentarioPadre)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.CarnetUsuario)
                .IsRequired();
            entity.HasIndex(e => new
            {
                e.IdGrupoEquipo,
                e.FechaCreacion,
                e.EstadoEliminado,
            });
            entity.HasIndex(e => new { e.IdComentarioPadre, e.EstadoEliminado });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.ToTable("notificaciones");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_notificacion");
            entity.Property(e => e.CarnetUsuario).HasColumnName("carnet_usuario").HasMaxLength(20);
            entity.Property(e => e.Tipo).HasColumnName("tipo").HasMaxLength(50);
            entity.Property(e => e.Titulo).HasColumnName("titulo");
            entity.Property(e => e.Contenido).HasColumnName("contenido");
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.Leido).HasColumnName("leido");
            entity.Property(e => e.FechaEnvio).HasColumnName("fecha_envio");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasIndex(e => new
            {
                e.CarnetUsuario,
                e.Leido,
                e.EstadoEliminado,
            });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });

        modelBuilder.Entity<AvisoDisponibilidad>(entity =>
        {
            entity.ToTable("avisos_disponibilidad");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_aviso");
            entity.Property(e => e.CarnetUsuario).HasColumnName("carnet_usuario").HasMaxLength(20);
            entity.Property(e => e.IdGrupoEquipo).HasColumnName("id_grupo_equipo");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.Notificado).HasColumnName("notificado");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.EstadoEliminado).HasColumnName(EstadoEliminadoCol);
            entity.HasIndex(e => new { e.Notificado, e.EstadoEliminado });
            entity.HasQueryFilter(e => !e.EstadoEliminado);
        });
    }
}
