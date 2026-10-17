using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HospitalTurnos.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarMedicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medicos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    matricula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    especialidad_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_personal_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medicos", x => x.id);
                    table.ForeignKey(
                        name: "fk_medicos_asp_net_users_usuario_personal_id",
                        column: x => x.usuario_personal_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medicos_especialidades_especialidad_id",
                        column: x => x.especialidad_id,
                        principalTable: "especialidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_medicos_especialidad_id",
                table: "medicos",
                column: "especialidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_medicos_matricula",
                table: "medicos",
                column: "matricula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medicos_usuario_personal_id",
                table: "medicos",
                column: "usuario_personal_id",
                unique: true);

            // Los médicos no se borran (FR-13 sólo pide alta y edición). 01-roles.sh le da DELETE por defecto
            // a toda tabla nueva: por eso REVOKE ALL y después sólo lo que usa.
            migrationBuilder.Sql("""
                REVOKE ALL ON TABLE medicos FROM turnos_app;
                GRANT SELECT, INSERT, UPDATE ON TABLE medicos TO turnos_app;
                DO $$ BEGIN
                  EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app',
                                 pg_get_serial_sequence('medicos', 'id'));
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "medicos");
        }
    }
}
