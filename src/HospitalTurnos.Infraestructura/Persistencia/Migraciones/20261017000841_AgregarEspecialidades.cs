using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HospitalTurnos.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarEspecialidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "especialidades",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_especialidades", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_especialidades_nombre_normalizado",
                table: "especialidades",
                column: "nombre_normalizado",
                unique: true);

            // La app crea, lista, desactiva y reactiva; nunca borra (los médicos y los turnos apuntan acá).
            // 01-roles.sh le da DELETE por defecto a toda tabla nueva: por eso REVOKE ALL y después lo justo.
            migrationBuilder.Sql("""
                REVOKE ALL ON TABLE especialidades FROM turnos_app;
                GRANT SELECT, INSERT, UPDATE ON TABLE especialidades TO turnos_app;
                DO $$ BEGIN
                  EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app',
                                 pg_get_serial_sequence('especialidades', 'id'));
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "especialidades");
        }
    }
}
