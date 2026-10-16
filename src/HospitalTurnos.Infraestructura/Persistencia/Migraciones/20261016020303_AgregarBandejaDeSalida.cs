using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HospitalTurnos.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarBandejaDeSalida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mensajes_salientes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    para = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    asunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cuerpo_texto = table.Column<string>(type: "text", nullable: false),
                    cuerpo_html = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    referencia_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    creado_el = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    proximo_intento_el = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    enviado_el = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fallo_definitivo_el = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    manejado_el = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultimo_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mensajes_salientes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mensajes_salientes_proximo_intento_el",
                table: "mensajes_salientes",
                column: "proximo_intento_el",
                filter: "enviado_el IS NULL AND fallo_definitivo_el IS NULL");

            // La aplicación inserta (Encolar) y actualiza (procesador); nunca borra. 01-roles.sh le da DELETE
            // por defecto a toda tabla nueva: por eso REVOKE ALL y después sólo lo que usa.
            // (SELECT … FOR UPDATE necesita privilegio UPDATE: ya está.)
            migrationBuilder.Sql("""
                REVOKE ALL ON TABLE mensajes_salientes FROM turnos_app;
                GRANT SELECT, INSERT, UPDATE ON TABLE mensajes_salientes TO turnos_app;
                DO $$ BEGIN
                  EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app',
                                 pg_get_serial_sequence('mensajes_salientes', 'id'));
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mensajes_salientes");
        }
    }
}
