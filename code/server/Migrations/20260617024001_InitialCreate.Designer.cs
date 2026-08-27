
using System;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IMT_Reservas.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260617024001_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "estado_equipo", "estado_equipo", new[] { "operativo", "parcialmente_operativo", "inoperativo" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "estado_prestamo", "estado_prestamo", new[] { "pendiente", "aprobado", "activo", "rechazado", "finalizado", "cancelado", "atrasado" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "tipo_usuario", "tipo_usuario", new[] { "docente", "administrador", "estudiante" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Accesorio", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_accesorio");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Descripcion")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("descripcion");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<int>("IdEquipo")
                        .HasColumnType("integer")
                        .HasColumnName("id_equipo");

                    b.Property<string>("Modelo")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("modelo");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("nombre");

                    b.Property<double?>("Precio")
                        .HasColumnType("double precision")
                        .HasColumnName("precio");

                    b.Property<string>("Tipo")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("tipo");

                    b.Property<string>("UrlDataSheet")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("url_data_sheet");

                    b.HasKey("Id");

                    b.HasIndex("IdEquipo");

                    b.HasIndex("Nombre", "IdEquipo", "EstadoEliminado");

                    b.ToTable("accesorios", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.AuditLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Accion")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("accion");

                    b.Property<string>("AdminCarnet")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("admin_carnet");

                    b.Property<string>("AdminNombre")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("admin_nombre");

                    b.Property<string>("Detalle")
                        .HasColumnType("text")
                        .HasColumnName("detalle");

                    b.Property<string>("Entidad")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("entidad");

                    b.Property<string>("EntidadId")
                        .HasColumnType("text")
                        .HasColumnName("entidad_id");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("timestamp");

                    b.HasKey("Id");

                    b.HasIndex("AdminCarnet", "Timestamp");

                    b.HasIndex("Entidad", "EntidadId");

                    b.ToTable("audit_logs", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Carrera", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_carrera");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("nombre");

                    b.HasKey("Id");

                    b.HasIndex("Nombre", "EstadoEliminado");

                    b.ToTable("carreras", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Categoria", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_categoria");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("nombre");

                    b.HasKey("Id");

                    b.HasIndex("Nombre", "EstadoEliminado");

                    b.ToTable("categorias", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Componente", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_componente");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Descripcion")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("descripcion");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<int>("IdEquipo")
                        .HasColumnType("integer")
                        .HasColumnName("id_equipo");

                    b.Property<string>("Modelo")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("modelo");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("nombre");

                    b.Property<double?>("PrecioReferencia")
                        .HasColumnType("double precision")
                        .HasColumnName("precio_referencia");

                    b.Property<string>("Tipo")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("tipo");

                    b.Property<string>("UrlDataSheet")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("url_data_sheet");

                    b.HasKey("Id");

                    b.HasIndex("IdEquipo");

                    b.HasIndex("Nombre", "IdEquipo", "EstadoEliminado");

                    b.ToTable("componentes", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Contrato", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ContratoHtml")
                        .HasColumnType("text")
                        .HasColumnName("contrato");

                    b.HasKey("Id");

                    b.ToTable("contratos", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.DetalleMantenimiento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_detalle_mantenimiento");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Descripcion")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("descripcion");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<int>("IdEquipo")
                        .HasColumnType("integer")
                        .HasColumnName("id_equipo");

                    b.Property<int>("IdMantenimiento")
                        .HasColumnType("integer")
                        .HasColumnName("id_mantenimiento");

                    b.Property<string>("TipoMantenimiento")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("tipo_mantenimiento");

                    b.HasKey("Id");

                    b.HasIndex("IdEquipo");

                    b.HasIndex("IdMantenimiento", "EstadoEliminado");

                    b.ToTable("detalles_mantenimientos", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.DetallePrestamo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_detalle_prestamo");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<EstadoEquipo?>("EstadoEquipoRetorno")
                        .HasColumnType("estado_equipo")
                        .HasColumnName("estado_equipo_retorno");

                    b.Property<int?>("IdEquipo")
                        .HasColumnType("integer")
                        .HasColumnName("id_equipo");

                    b.Property<int>("IdGrupoEquipo")
                        .HasColumnType("integer")
                        .HasColumnName("id_grupo_equipo");

                    b.Property<int>("IdPrestamo")
                        .HasColumnType("integer")
                        .HasColumnName("id_prestamo");

                    b.HasKey("Id");

                    b.HasIndex("IdEquipo");

                    b.HasIndex("IdGrupoEquipo");

                    b.HasIndex("IdPrestamo", "EstadoEliminado");

                    b.ToTable("detalles_prestamos", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.EmpresaMantenimiento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_empresa_mantenimiento");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ApellidoResponsable")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("apellido_responsable");

                    b.Property<string>("Direccion")
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("direccion");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<string>("Nit")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("nit");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("nombre");

                    b.Property<string>("NombreResponsable")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("nombre_responsable");

                    b.Property<string>("Telefono")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("telefono");

                    b.HasKey("Id");

                    b.HasIndex("Nombre", "EstadoEliminado");

                    b.ToTable("empresas_mantenimiento", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Equipo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_equipo");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CodigoImt")
                        .HasColumnType("integer")
                        .HasColumnName("codigo_imt");

                    b.Property<string>("CodigoUcb")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("codigo_ucb");

                    b.Property<double?>("CostoReferencia")
                        .HasColumnType("double precision")
                        .HasColumnName("costo_referencia");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("descripcion");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<EstadoEquipo>("EstadoEquipo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("estado_equipo")
                        .HasDefaultValue(EstadoEquipo.Operativo)
                        .HasColumnName("estado_equipo");

                    b.Property<DateOnly>("FechaIngresoEquipo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("date")
                        .HasDefaultValue(new DateOnly(2026, 6, 17))
                        .HasColumnName("fecha_ingreso_equipo");

                    b.Property<int?>("IdGavetero")
                        .HasColumnType("integer")
                        .HasColumnName("id_gavetero");

                    b.Property<int>("IdGrupoEquipo")
                        .HasColumnType("integer")
                        .HasColumnName("id_grupo_equipo");

                    b.Property<string>("NumeroSerial")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("numero_serial");

                    b.Property<string>("Procedencia")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("procedencia");

                    b.Property<int?>("TiempoMaximoPrestamo")
                        .HasColumnType("integer")
                        .HasColumnName("tiempo_max_prestamo");

                    b.Property<string>("Ubicacion")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("ubicacion");

                    b.HasKey("Id");

                    b.HasIndex("CodigoImt")
                        .IsUnique();

                    b.HasIndex("IdGavetero");

                    b.HasIndex("IdGrupoEquipo", "CodigoImt", "EstadoEliminado");

                    b.ToTable("equipos", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Gavetero", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_gavetero");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<double?>("Altura")
                        .HasColumnType("double precision")
                        .HasColumnName("altura");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<int>("IdMueble")
                        .HasColumnType("integer")
                        .HasColumnName("id_mueble");

                    b.Property<double?>("Longitud")
                        .HasColumnType("double precision")
                        .HasColumnName("longitud");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("nombre");

                    b.Property<double?>("Profundidad")
                        .HasColumnType("double precision")
                        .HasColumnName("profundidad");

                    b.Property<string>("Tipo")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("tipo");

                    b.HasKey("Id");

                    b.HasIndex("IdMueble");

                    b.HasIndex("Nombre")
                        .IsUnique();

                    b.HasIndex("Nombre", "IdMueble", "EstadoEliminado");

                    b.ToTable("gaveteros", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.GrupoEquipo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_grupo_equipo");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Cantidad")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("cantidad");

                    b.Property<decimal?>("CostoPromedio")
                        .HasPrecision(10, 2)
                        .HasColumnType("numeric(10,2)")
                        .HasColumnName("costo_promedio");

                    b.Property<string>("Descripcion")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("descripcion");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<int>("IdCategoria")
                        .HasColumnType("integer")
                        .HasColumnName("id_categoria");

                    b.Property<string>("Marca")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("marca");

                    b.Property<string>("Modelo")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("modelo");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("nombre");

                    b.Property<string>("UrlDataSheet")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("url_data_sheet");

                    b.Property<string>("UrlImagen")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("url_imagen");

                    b.HasKey("Id");

                    b.HasIndex("Nombre", "Modelo", "Marca")
                        .IsUnique();

                    b.HasIndex("IdCategoria", "Nombre", "Modelo", "Marca", "EstadoEliminado");

                    b.ToTable("grupos_equipos", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Mantenimiento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_mantenimiento");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<double?>("Costo")
                        .HasColumnType("double precision")
                        .HasColumnName("costo");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("descripcion");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<DateTime>("FechaFinalMantenimiento")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("fecha_final_mantenimiento");

                    b.Property<DateTime>("FechaMantenimiento")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("fecha_mantenimiento");

                    b.Property<int>("IdEmpresa")
                        .HasColumnType("integer")
                        .HasColumnName("id_empresa");

                    b.HasKey("Id");

                    b.HasIndex("IdEmpresa");

                    b.HasIndex("FechaMantenimiento", "FechaFinalMantenimiento", "IdEmpresa", "EstadoEliminado");

                    b.ToTable("mantenimientos", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Mueble", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_mueble");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<double?>("Altura")
                        .HasColumnType("double precision")
                        .HasColumnName("altura");

                    b.Property<double?>("Costo")
                        .HasColumnType("double precision")
                        .HasColumnName("costo");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<double?>("Longitud")
                        .HasColumnType("double precision")
                        .HasColumnName("longitud");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("nombre");

                    b.Property<int>("NumeroGaveteros")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("numero_gaveteros");

                    b.Property<double?>("Profundidad")
                        .HasColumnType("double precision")
                        .HasColumnName("profundidad");

                    b.Property<string>("Tipo")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("tipo");

                    b.Property<string>("Ubicacion")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("ubicacion");

                    b.HasKey("Id");

                    b.HasIndex("Nombre")
                        .IsUnique();

                    b.HasIndex("Nombre", "EstadoEliminado");

                    b.ToTable("muebles", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Prestamo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id_prestamo");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Carnet")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("carnet");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<EstadoPrestamo>("EstadoPrestamo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("estado_prestamo")
                        .HasDefaultValue(EstadoPrestamo.Pendiente)
                        .HasColumnName("estado_prestamo");

                    b.Property<DateTime?>("FechaDevolucion")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("fecha_devolucion");

                    b.Property<DateTime>("FechaDevolucionEsperada")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("fecha_devolucion_esperada");

                    b.Property<DateTime?>("FechaPrestamo")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("fecha_prestamo");

                    b.Property<DateTime>("FechaPrestamoEsperada")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("fecha_prestamo_esperada");

                    b.Property<DateTime>("FechaSolicitud")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("fecha_solicitud")
                        .HasDefaultValueSql("now() AT TIME ZONE 'America/La_Paz'");

                    b.Property<int?>("IdContrato")
                        .HasColumnType("integer")
                        .HasColumnName("id_contrato");

                    b.Property<string>("Observacion")
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)")
                        .HasColumnName("observacion");

                    b.HasKey("Id");

                    b.HasIndex("IdContrato");

                    b.HasIndex("Carnet", "EstadoPrestamo", "EstadoEliminado");

                    b.HasIndex("FechaPrestamoEsperada", "FechaDevolucionEsperada", "Carnet", "EstadoEliminado");

                    b.ToTable("prestamos", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Usuario", b =>
                {
                    b.Property<string>("Carnet")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("carnet");

                    b.Property<string>("ApellidoMaterno")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("apellido_materno");

                    b.Property<string>("ApellidoPaterno")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("apellido_paterno");

                    b.Property<string>("Contrasena")
                        .IsRequired()
                        .HasMaxLength(72)
                        .HasColumnType("character varying(72)")
                        .HasColumnName("contrasena");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("email");

                    b.Property<string>("EmailReferencia")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("email_referencia");

                    b.Property<bool>("EstadoEliminado")
                        .HasColumnType("boolean")
                        .HasColumnName("estado_eliminado");

                    b.Property<int>("IdCarrera")
                        .HasColumnType("integer")
                        .HasColumnName("id_carrera");

                    b.Property<byte[]>("ImagenAtrasCarnet")
                        .HasColumnType("bytea")
                        .HasColumnName("imagen_atras_carnet");

                    b.Property<byte[]>("ImagenFrenteCarnet")
                        .HasColumnType("bytea")
                        .HasColumnName("imagen_frente_carnet");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("nombre");

                    b.Property<string>("NombreReferencia")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("nombre_referencia");

                    b.Property<string>("RefreshToken")
                        .HasColumnType("text")
                        .HasColumnName("refresh_token");

                    b.Property<DateTime?>("RefreshTokenExpiry")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("refresh_token_expiry");

                    b.Property<TipoUsuario>("Rol")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tipo_usuario")
                        .HasDefaultValue(TipoUsuario.Estudiante)
                        .HasColumnName("rol");

                    b.Property<string>("Telefono")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("telefono");

                    b.Property<string>("TelefonoReferencia")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("telefono_referencia");

                    b.HasKey("Carnet");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("IdCarrera");

                    b.HasIndex("RefreshToken");

                    b.HasIndex("Nombre", "EstadoEliminado");

                    b.ToTable("usuarios", (string)null);
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Accesorio", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Equipo", null)
                        .WithMany()
                        .HasForeignKey("IdEquipo")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Componente", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Equipo", null)
                        .WithMany()
                        .HasForeignKey("IdEquipo")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.DetalleMantenimiento", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Equipo", null)
                        .WithMany()
                        .HasForeignKey("IdEquipo")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("IMT_Reservas.Server.Core.Entities.Mantenimiento", null)
                        .WithMany()
                        .HasForeignKey("IdMantenimiento")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.DetallePrestamo", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Equipo", null)
                        .WithMany()
                        .HasForeignKey("IdEquipo");

                    b.HasOne("IMT_Reservas.Server.Core.Entities.GrupoEquipo", null)
                        .WithMany()
                        .HasForeignKey("IdGrupoEquipo")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("IMT_Reservas.Server.Core.Entities.Prestamo", null)
                        .WithMany()
                        .HasForeignKey("IdPrestamo")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Equipo", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Gavetero", "Gavetero")
                        .WithMany()
                        .HasForeignKey("IdGavetero");

                    b.HasOne("IMT_Reservas.Server.Core.Entities.GrupoEquipo", "GrupoEquipo")
                        .WithMany()
                        .HasForeignKey("IdGrupoEquipo")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Gavetero");

                    b.Navigation("GrupoEquipo");
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Gavetero", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Mueble", "Mueble")
                        .WithMany()
                        .HasForeignKey("IdMueble")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Mueble");
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.GrupoEquipo", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Categoria", "Categoria")
                        .WithMany()
                        .HasForeignKey("IdCategoria")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Categoria");
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Mantenimiento", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.EmpresaMantenimiento", null)
                        .WithMany()
                        .HasForeignKey("IdEmpresa")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Prestamo", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Usuario", null)
                        .WithMany()
                        .HasForeignKey("Carnet")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("IMT_Reservas.Server.Core.Entities.Contrato", null)
                        .WithMany()
                        .HasForeignKey("IdContrato");
                });

            modelBuilder.Entity("IMT_Reservas.Server.Core.Entities.Usuario", b =>
                {
                    b.HasOne("IMT_Reservas.Server.Core.Entities.Carrera", null)
                        .WithMany()
                        .HasForeignKey("IdCarrera")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
