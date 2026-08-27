using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMT_Reservas.Server.src.Infrastructure.Config.Migrations
{
    public partial class OptimizePrestamoQueries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_detalles_prestamos_id_grupo_equipo",
                table: "detalles_prestamos");

            migrationBuilder.DropIndex(
                name: "IX_detalles_mantenimientos_id_equipo",
                table: "detalles_mantenimientos");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "fecha_ingreso_equipo",
                table: "equipos",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE",
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldDefaultValue: new DateOnly(2026, 8, 25));

            migrationBuilder.CreateIndex(
                name: "IX_prestamos_estado_eliminado_fecha_solicitud_id_prestamo",
                table: "prestamos",
                columns: new[] { "estado_eliminado", "fecha_solicitud", "id_prestamo" });

            migrationBuilder.CreateIndex(
                name: "IX_prestamos_estado_prestamo_estado_eliminado_fecha_devolucion~",
                table: "prestamos",
                columns: new[] { "estado_prestamo", "estado_eliminado", "fecha_devolucion_esperada" });

            migrationBuilder.CreateIndex(
                name: "IX_prestamos_estado_prestamo_estado_eliminado_fecha_prestamo_e~",
                table: "prestamos",
                columns: new[] { "estado_prestamo", "estado_eliminado", "fecha_prestamo_esperada" });

            migrationBuilder.CreateIndex(
                name: "IX_prestamos_estado_prestamo_recordatorio_enviado_estado_elimi~",
                table: "prestamos",
                columns: new[] { "estado_prestamo", "recordatorio_enviado", "estado_eliminado", "fecha_devolucion_esperada" });

            migrationBuilder.CreateIndex(
                name: "IX_equipos_id_grupo_equipo_estado_equipo_estado_eliminado",
                table: "equipos",
                columns: new[] { "id_grupo_equipo", "estado_equipo", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_detalles_prestamos_id_grupo_equipo_id_equipo_estado_elimina~",
                table: "detalles_prestamos",
                columns: new[] { "id_grupo_equipo", "id_equipo", "estado_eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_detalles_mantenimientos_id_equipo_estado_eliminado",
                table: "detalles_mantenimientos",
                columns: new[] { "id_equipo", "estado_eliminado" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_prestamos_estado_eliminado_fecha_solicitud_id_prestamo",
                table: "prestamos");

            migrationBuilder.DropIndex(
                name: "IX_prestamos_estado_prestamo_estado_eliminado_fecha_devolucion~",
                table: "prestamos");

            migrationBuilder.DropIndex(
                name: "IX_prestamos_estado_prestamo_estado_eliminado_fecha_prestamo_e~",
                table: "prestamos");

            migrationBuilder.DropIndex(
                name: "IX_prestamos_estado_prestamo_recordatorio_enviado_estado_elimi~",
                table: "prestamos");

            migrationBuilder.DropIndex(
                name: "IX_equipos_id_grupo_equipo_estado_equipo_estado_eliminado",
                table: "equipos");

            migrationBuilder.DropIndex(
                name: "IX_detalles_prestamos_id_grupo_equipo_id_equipo_estado_elimina~",
                table: "detalles_prestamos");

            migrationBuilder.DropIndex(
                name: "IX_detalles_mantenimientos_id_equipo_estado_eliminado",
                table: "detalles_mantenimientos");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "fecha_ingreso_equipo",
                table: "equipos",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2026, 8, 25),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldDefaultValueSql: "CURRENT_DATE");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_prestamos_id_grupo_equipo",
                table: "detalles_prestamos",
                column: "id_grupo_equipo");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_mantenimientos_id_equipo",
                table: "detalles_mantenimientos",
                column: "id_equipo");
        }
    }
}
