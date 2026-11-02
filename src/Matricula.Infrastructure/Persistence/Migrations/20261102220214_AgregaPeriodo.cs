using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Matricula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaPeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "periodo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    inicio_clases = table.Column<DateOnly>(type: "date", nullable: false),
                    fin_clases = table.Column<DateOnly>(type: "date", nullable: false),
                    inicio_matricula = table.Column<DateOnly>(type: "date", nullable: false),
                    fin_matricula = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_limite_retiro = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_periodo", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_periodo_nombre",
                table: "periodo",
                column: "nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "periodo");
        }
    }
}
