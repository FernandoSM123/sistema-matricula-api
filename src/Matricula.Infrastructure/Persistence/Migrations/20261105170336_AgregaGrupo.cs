using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Matricula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaGrupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grupo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nrc = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    curso_id = table.Column<int>(type: "integer", nullable: false),
                    profesor_id = table.Column<int>(type: "integer", nullable: false),
                    periodo_id = table.Column<int>(type: "integer", nullable: false),
                    aula_id = table.Column<int>(type: "integer", nullable: true),
                    cupo = table.Column<int>(type: "integer", nullable: false),
                    modalidad = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cupos_ocupados = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grupo", x => x.id);
                    table.ForeignKey(
                        name: "fk_grupo_aula_aula_id",
                        column: x => x.aula_id,
                        principalTable: "aula",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grupo_curso_curso_id",
                        column: x => x.curso_id,
                        principalTable: "curso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grupo_periodos_periodo_id",
                        column: x => x.periodo_id,
                        principalTable: "periodo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grupo_profesores_profesor_id",
                        column: x => x.profesor_id,
                        principalTable: "profesor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "horario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grupo_id = table.Column<int>(type: "integer", nullable: false),
                    dia = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_horario", x => x.id);
                    table.ForeignKey(
                        name: "fk_horario_grupo_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "grupo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_grupo_aula_id",
                table: "grupo",
                column: "aula_id");

            migrationBuilder.CreateIndex(
                name: "ix_grupo_curso_id",
                table: "grupo",
                column: "curso_id");

            migrationBuilder.CreateIndex(
                name: "ix_grupo_nrc",
                table: "grupo",
                column: "nrc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grupo_periodo_id_aula_id",
                table: "grupo",
                columns: new[] { "periodo_id", "aula_id" });

            migrationBuilder.CreateIndex(
                name: "ix_grupo_periodo_id_profesor_id",
                table: "grupo",
                columns: new[] { "periodo_id", "profesor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_grupo_profesor_id",
                table: "grupo",
                column: "profesor_id");

            migrationBuilder.CreateIndex(
                name: "ix_horario_grupo_id",
                table: "horario",
                column: "grupo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "horario");

            migrationBuilder.DropTable(
                name: "grupo");
        }
    }
}
