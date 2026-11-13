using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Matricula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaRegistroError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registro_error",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    origen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tipo_excepcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    detalle = table.Column<string>(type: "text", nullable: false),
                    metodo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ruta = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    usuario_rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registro_error", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registro_error_fecha",
                table: "registro_error",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_registro_error_trace_id",
                table: "registro_error",
                column: "trace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registro_error");
        }
    }
}
