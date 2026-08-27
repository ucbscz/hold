using System;
using IMT_Reservas.Server.Core.Entities;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IMT_Reservas.Server.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:estado_equipo.estado_equipo", "operativo,parcialmente_operativo,inoperativo")
                .Annotation("Npgsql:Enum:estado_prestamo.estado_prestamo", "pendiente,aprobado,activo,rechazado,finalizado,cancelado,atrasado")
                .Annotation("Npgsql:Enum:tipo_usuario.tipo_usuario", "docente,administrador,estudiante");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admin_carnet = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    admin_nombre = table.Column<string>(type: "text", nullable: false),
                    accion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidad_id = table.Column<string>(type: "text", nullable: true),
                    detalle = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "carreras",
                columns: table => new
                {
                    id_carrera = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carreras", x => x.id_carrera);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id_categoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id_categoria);
                });

            migrationBuilder.CreateTable(
                name: "contratos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contrato = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contratos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empresas_mantenimiento",
                columns: table => new
                {
                    id_empresa_mantenimiento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    nit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    direccion = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    telefono = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    nombre_responsable = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    apellido_responsable = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas_mantenimiento", x => x.id_empresa_mantenimiento);
                });

            migrationBuilder.CreateTable(
                name: "muebles",
                columns: table => new
                {
                    id_mueble = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ubicacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    numero_gaveteros = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    longitud = table.Column<double>(type: "double precision", nullable: true),
                    profundidad = table.Column<double>(type: "double precision", nullable: true),
                    altura = table.Column<double>(type: "double precision", nullable: true),
                    costo = table.Column<double>(type: "double precision", nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_muebles", x => x.id_mueble);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    carnet = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    apellido_paterno = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    apellido_materno = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    rol = table.Column<TipoUsuario>(type: "tipo_usuario", nullable: false, defaultValue: TipoUsuario.Estudiante),
                    contrasena = table.Column<string>(type: "character varying(72)", maxLength: 72, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    telefono = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    telefono_referencia = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    nombre_referencia = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    email_referencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    id_carrera = table.Column<int>(type: "integer", nullable: false),
                    imagen_frente_carnet = table.Column<byte[]>(type: "bytea", nullable: true),
                    imagen_atras_carnet = table.Column<byte[]>(type: "bytea", nullable: true),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    refresh_token_expiry = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.carnet);
                    table.ForeignKey(
                        name: "FK_usuarios_carreras_id_carrera",
                        column: x => x.id_carrera,
                        principalTable: "carreras",
                        principalColumn: "id_carrera",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grupos_equipos",
                columns: table => new
                {
                    id_grupo_equipo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modelo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    marca = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    id_categoria = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    url_imagen = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    url_data_sheet = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    cantidad = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    costo_promedio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos_equipos", x => x.id_grupo_equipo);
                    table.ForeignKey(
                        name: "FK_grupos_equipos_categorias_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categorias",
                        principalColumn: "id_categoria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mantenimientos",
                columns: table => new
                {
                    id_mantenimiento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    fecha_mantenimiento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    fecha_final_mantenimiento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    costo = table.Column<double>(type: "double precision", nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mantenimientos", x => x.id_mantenimiento);
                    table.ForeignKey(
                        name: "FK_mantenimientos_empresas_mantenimiento_id_empresa",
                        column: x => x.id_empresa,
                        principalTable: "empresas_mantenimiento",
                        principalColumn: "id_empresa_mantenimiento",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gaveteros",
                columns: table => new
                {
                    id_gavetero = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    id_mueble = table.Column<int>(type: "integer", nullable: false),
                    longitud = table.Column<double>(type: "double precision", nullable: true),
                    profundidad = table.Column<double>(type: "double precision", nullable: true),
                    altura = table.Column<double>(type: "double precision", nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gaveteros", x => x.id_gavetero);
                    table.ForeignKey(
                        name: "FK_gaveteros_muebles_id_mueble",
                        column: x => x.id_mueble,
                        principalTable: "muebles",
                        principalColumn: "id_mueble",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prestamos",
                columns: table => new
                {
                    id_prestamo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha_solicitud = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now() AT TIME ZONE 'America/La_Paz'"),
                    fecha_prestamo_esperada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    fecha_prestamo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    fecha_devolucion_esperada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    fecha_devolucion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    carnet = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado_prestamo = table.Column<EstadoPrestamo>(type: "estado_prestamo", nullable: false, defaultValue: EstadoPrestamo.Pendiente),
                    observacion = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    id_contrato = table.Column<int>(type: "integer", nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prestamos", x => x.id_prestamo);
                    table.ForeignKey(
                        name: "FK_prestamos_contratos_id_contrato",
                        column: x => x.id_contrato,
                        principalTable: "contratos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_prestamos_usuarios_carnet",
                        column: x => x.carnet,
                        principalTable: "usuarios",
                        principalColumn: "carnet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "equipos",
                columns: table => new
                {
                    id_equipo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_grupo_equipo = table.Column<int>(type: "integer", nullable: false),
                    codigo_imt = table.Column<int>(type: "integer", nullable: false),
                    id_gavetero = table.Column<int>(type: "integer", nullable: true),
                    codigo_ucb = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    numero_serial = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    estado_equipo = table.Column<EstadoEquipo>(type: "estado_equipo", nullable: false, defaultValue: EstadoEquipo.Operativo),
                    ubicacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    costo_referencia = table.Column<double>(type: "double precision", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    tiempo_max_prestamo = table.Column<int>(type: "integer", nullable: true),
                    procedencia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    fecha_ingreso_equipo = table.Column<DateOnly>(type: "date", nullable: false, defaultValue: new DateOnly(2026, 6, 17)),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipos", x => x.id_equipo);
                    table.ForeignKey(
                        name: "FK_equipos_gaveteros_id_gavetero",
                        column: x => x.id_gavetero,
                        principalTable: "gaveteros",
                        principalColumn: "id_gavetero");
                    table.ForeignKey(
                        name: "FK_equipos_grupos_equipos_id_grupo_equipo",
                        column: x => x.id_grupo_equipo,
                        principalTable: "grupos_equipos",
                        principalColumn: "id_grupo_equipo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accesorios",
                columns: table => new
                {
                    id_accesorio = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    modelo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    url_data_sheet = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    precio = table.Column<double>(type: "double precision", nullable: true),
                    id_equipo = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accesorios", x => x.id_accesorio);
                    table.ForeignKey(
                        name: "FK_accesorios_equipos_id_equipo",
                        column: x => x.id_equipo,
                        principalTable: "equipos",
                        principalColumn: "id_equipo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "componentes",
                columns: table => new
                {
                    id_componente = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    modelo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    precio_referencia = table.Column<double>(type: "double precision", nullable: true),
                    id_equipo = table.Column<int>(type: "integer", nullable: false),
                    url_data_sheet = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_componentes", x => x.id_componente);
                    table.ForeignKey(
                        name: "FK_componentes_equipos_id_equipo",
                        column: x => x.id_equipo,
                        principalTable: "equipos",
                        principalColumn: "id_equipo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detalles_mantenimientos",
                columns: table => new
                {
                    id_detalle_mantenimiento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_mantenimiento = table.Column<int>(type: "integer", nullable: false),
                    id_equipo = table.Column<int>(type: "integer", nullable: false),
                    tipo_mantenimiento = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalles_mantenimientos", x => x.id_detalle_mantenimiento);
                    table.ForeignKey(
                        name: "FK_detalles_mantenimientos_equipos_id_equipo",
                        column: x => x.id_equipo,
                        principalTable: "equipos",
                        principalColumn: "id_equipo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detalles_mantenimientos_mantenimientos_id_mantenimiento",
                        column: x => x.id_mantenimiento,
                        principalTable: "mantenimientos",
                        principalColumn: "id_mantenimiento",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detalles_prestamos",
                columns: table => new
                {
                    id_detalle_prestamo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_prestamo = table.Column<int>(type: "integer", nullable: false),
                    id_equipo = table.Column<int>(type: "integer", nullable: true),
                    id_grupo_equipo = table.Column<int>(type: "integer", nullable: false),
                    estado_equipo_retorno = table.Column<EstadoEquipo>(type: "estado_equipo", nullable: true),
                    estado_eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalles_prestamos", x => x.id_detalle_prestamo);
                    table.ForeignKey(
                        name: "FK_detalles_prestamos_equipos_id_equipo",
                        column: x => x.id_equipo,
                        principalTable: "equipos",
                        principalColumn: "id_equipo");
                    table.ForeignKey(
                        name: "FK_detalles_prestamos_grupos_equipos_id_grupo_equipo",
                        column: x => x.id_grupo_equipo,
                        principalTable: "grupos_equipos",
                        principalColumn: "id_grupo_equipo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detalles_prestamos_prestamos_id_prestamo",
                        column: x => x.id_prestamo,
                        principalTable: "prestamos",
                        principalColumn: "id_prestamo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accesorios_id_equipo",
                table: "accesorios",
                column: "id_equipo");

            migrationBuilder.CreateIndex(
                name: "IX_accesorios_nombre_id_equipo_estado_eliminado",
                table: "accesorios",
                columns: new[] { "nombre", "id_equipo", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_admin_carnet_timestamp",
                table: "audit_logs",
                columns: new[] { "admin_carnet", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entidad_entidad_id",
                table: "audit_logs",
                columns: new[] { "entidad", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "IX_carreras_nombre_estado_eliminado",
                table: "carreras",
                columns: new[] { "nombre", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_nombre_estado_eliminado",
                table: "categorias",
                columns: new[] { "nombre", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_componentes_id_equipo",
                table: "componentes",
                column: "id_equipo");

            migrationBuilder.CreateIndex(
                name: "IX_componentes_nombre_id_equipo_estado_eliminado",
                table: "componentes",
                columns: new[] { "nombre", "id_equipo", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_detalles_mantenimientos_id_equipo",
                table: "detalles_mantenimientos",
                column: "id_equipo");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_mantenimientos_id_mantenimiento_estado_eliminado",
                table: "detalles_mantenimientos",
                columns: new[] { "id_mantenimiento", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_detalles_prestamos_id_equipo",
                table: "detalles_prestamos",
                column: "id_equipo");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_prestamos_id_grupo_equipo",
                table: "detalles_prestamos",
                column: "id_grupo_equipo");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_prestamos_id_prestamo_estado_eliminado",
                table: "detalles_prestamos",
                columns: new[] { "id_prestamo", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_empresas_mantenimiento_nombre_estado_eliminado",
                table: "empresas_mantenimiento",
                columns: new[] { "nombre", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_equipos_codigo_imt",
                table: "equipos",
                column: "codigo_imt",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_equipos_id_gavetero",
                table: "equipos",
                column: "id_gavetero");

            migrationBuilder.CreateIndex(
                name: "IX_equipos_id_grupo_equipo_codigo_imt_estado_eliminado",
                table: "equipos",
                columns: new[] { "id_grupo_equipo", "codigo_imt", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_gaveteros_id_mueble",
                table: "gaveteros",
                column: "id_mueble");

            migrationBuilder.CreateIndex(
                name: "IX_gaveteros_nombre",
                table: "gaveteros",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gaveteros_nombre_id_mueble_estado_eliminado",
                table: "gaveteros",
                columns: new[] { "nombre", "id_mueble", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_grupos_equipos_id_categoria_nombre_modelo_marca_estado_elim~",
                table: "grupos_equipos",
                columns: new[] { "id_categoria", "nombre", "modelo", "marca", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_grupos_equipos_nombre_modelo_marca",
                table: "grupos_equipos",
                columns: new[] { "nombre", "modelo", "marca" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mantenimientos_fecha_mantenimiento_fecha_final_mantenimient~",
                table: "mantenimientos",
                columns: new[] { "fecha_mantenimiento", "fecha_final_mantenimiento", "id_empresa", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_mantenimientos_id_empresa",
                table: "mantenimientos",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "IX_muebles_nombre",
                table: "muebles",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_muebles_nombre_estado_eliminado",
                table: "muebles",
                columns: new[] { "nombre", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_prestamos_carnet_estado_prestamo_estado_eliminado",
                table: "prestamos",
                columns: new[] { "carnet", "estado_prestamo", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_prestamos_fecha_prestamo_esperada_fecha_devolucion_esperada~",
                table: "prestamos",
                columns: new[] { "fecha_prestamo_esperada", "fecha_devolucion_esperada", "carnet", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_prestamos_id_contrato",
                table: "prestamos",
                column: "id_contrato");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_id_carrera",
                table: "usuarios",
                column: "id_carrera");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_nombre_estado_eliminado",
                table: "usuarios",
                columns: new[] { "nombre", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_refresh_token",
                table: "usuarios",
                column: "refresh_token");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accesorios");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "componentes");

            migrationBuilder.DropTable(
                name: "detalles_mantenimientos");

            migrationBuilder.DropTable(
                name: "detalles_prestamos");

            migrationBuilder.DropTable(
                name: "mantenimientos");

            migrationBuilder.DropTable(
                name: "equipos");

            migrationBuilder.DropTable(
                name: "prestamos");

            migrationBuilder.DropTable(
                name: "empresas_mantenimiento");

            migrationBuilder.DropTable(
                name: "gaveteros");

            migrationBuilder.DropTable(
                name: "grupos_equipos");

            migrationBuilder.DropTable(
                name: "contratos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "muebles");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "carreras");
        }
    }
}
