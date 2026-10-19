using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Matricula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaUsuarioYAdministrador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    temp_password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    temp_password_expira = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    temp_password_motivo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    security_stamp = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    origen_registro = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    creado_por_id = table.Column<int>(type: "integer", nullable: true),
                    ultimo_ingreso = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_cambio_contrasena = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuario_usuario_creado_por_id",
                        column: x => x.creado_por_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "administrador",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cedula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_administrador", x => x.id);
                    table.ForeignKey(
                        name: "fk_administrador_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_administrador_cedula",
                table: "administrador",
                column: "cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_administrador_usuario_id",
                table: "administrador",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_correo",
                table: "usuario",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_creado_por_id",
                table: "usuario",
                column: "creado_por_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "administrador");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
