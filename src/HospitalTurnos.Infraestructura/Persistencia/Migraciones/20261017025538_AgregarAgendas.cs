using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HospitalTurnos.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarAgendas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agendas_medico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    medico_id = table.Column<long>(type: "bigint", nullable: false),
                    duracion_turno_minutos = table.Column<int>(type: "integer", nullable: false),
                    vigente_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo_rechazo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    enviada_el = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    aprobada_el = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rechazada_el = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agendas_medico", x => x.id);
                    table.CheckConstraint("ck_agendas_medico_duracion", "duracion_turno_minutos BETWEEN 10 AND 60");
                    table.ForeignKey(
                        name: "fk_agendas_medico_medicos_medico_id",
                        column: x => x.medico_id,
                        principalTable: "medicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "franjas_agenda",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dia = table.Column<int>(type: "integer", nullable: false),
                    desde = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hasta = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    agenda_medico_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_franjas_agenda", x => x.id);
                    table.CheckConstraint("ck_franjas_agenda_dia", "dia BETWEEN 0 AND 6");
                    table.CheckConstraint("ck_franjas_agenda_horas", "hasta > desde OR hasta = '00:00'");
                    table.ForeignKey(
                        name: "fk_franjas_agenda_agendas_medico_agenda_medico_id",
                        column: x => x.agenda_medico_id,
                        principalTable: "agendas_medico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agendas_medico_medico_id_estado_vigente_desde",
                table: "agendas_medico",
                columns: new[] { "medico_id", "estado", "vigente_desde" });

            migrationBuilder.CreateIndex(
                name: "ux_agendas_medico_una_enviada",
                table: "agendas_medico",
                column: "medico_id",
                unique: true,
                filter: "estado = 'Enviada'");

            migrationBuilder.CreateIndex(
                name: "ix_franjas_agenda_agenda_medico_id",
                table: "franjas_agenda",
                column: "agenda_medico_id");

            // Las agendas se versionan (ADR-06): la app crea y cambia de estado, pero nunca borra. Las franjas sí:
            // quitar una franja de un borrador es un DELETE. 01-roles.sh da DELETE por defecto: por eso REVOKE + GRANT.
            migrationBuilder.Sql("""
                REVOKE ALL ON TABLE agendas_medico FROM turnos_app;
                GRANT SELECT, INSERT, UPDATE ON TABLE agendas_medico TO turnos_app;
                DO $$ BEGIN
                  EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app',
                                 pg_get_serial_sequence('agendas_medico', 'id'));
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "franjas_agenda");

            migrationBuilder.DropTable(
                name: "agendas_medico");
        }
    }
}
