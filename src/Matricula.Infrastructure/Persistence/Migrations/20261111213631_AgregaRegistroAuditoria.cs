using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Matricula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaRegistroAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registro_auditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entidad_id = table.Column<int>(type: "integer", nullable: false),
                    cambios = table.Column<string>(type: "jsonb", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    usuario_correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    usuario_rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    operacion_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registro_auditoria", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registro_auditoria_entidad_entidad_id",
                table: "registro_auditoria",
                columns: new[] { "entidad", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registro_auditoria_fecha",
                table: "registro_auditoria",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_registro_auditoria_usuario_id",
                table: "registro_auditoria",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registro_auditoria");
        }
    }
}
