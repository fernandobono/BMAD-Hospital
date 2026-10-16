using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HospitalTurnos.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarRegistroAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registros_auditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cuando = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    usuario_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quien = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidad_tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidad_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    detalle = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registros_auditoria", x => x.id);
                });

            // ADR-07: el registro se protege en la base. Ninguna migración futura modifica ni borra estas
            // filas, ni desactiva el disparador para hacerlo (regla de la story 1.3).
            migrationBuilder.Sql("""
                -- Por qué REVOKE ALL primero: 01-roles.sh configura ALTER DEFAULT PRIVILEGES para turnos_app,
                -- así que la tabla nace con UPDATE/DELETE. Revocar todo y otorgar sólo lo necesario no depende de eso.
                REVOKE ALL ON TABLE registros_auditoria FROM turnos_app;
                GRANT SELECT, INSERT ON TABLE registros_auditoria TO turnos_app;
                -- Inofensivo si la columna es identity; necesario si alguien la cambia a serial.
                DO $$ BEGIN
                  EXECUTE format('GRANT USAGE ON SEQUENCE %s TO turnos_app',
                                 pg_get_serial_sequence('registros_auditoria', 'id'));
                END $$;

                -- Los permisos frenan a la aplicación; el disparador rechaza UPDATE/DELETE/TRUNCATE de cualquier rol
                -- mientras esté activo, también del dueño. El dueño podría desactivarlo: por eso su contraseña
                -- no vive en el contenedor de la aplicación en producción (story 1.3, "Qué garantiza el disparador").
                CREATE FUNCTION rechazar_cambios_auditoria() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  RAISE EXCEPTION 'registros_auditoria es de sólo inserción: % no permitido', TG_OP
                    USING ERRCODE = 'P0001';
                END $$;

                CREATE TRIGGER auditoria_sin_update_ni_delete
                  BEFORE UPDATE OR DELETE ON registros_auditoria
                  FOR EACH ROW EXECUTE FUNCTION rechazar_cambios_auditoria();

                -- TRUNCATE no dispara los disparadores por fila: necesita uno por sentencia.
                CREATE TRIGGER auditoria_sin_truncate
                  BEFORE TRUNCATE ON registros_auditoria
                  FOR EACH STATEMENT EXECUTE FUNCTION rechazar_cambios_auditoria();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS auditoria_sin_truncate ON registros_auditoria;
                DROP TRIGGER IF EXISTS auditoria_sin_update_ni_delete ON registros_auditoria;
                DROP FUNCTION IF EXISTS rechazar_cambios_auditoria();
                """);

            migrationBuilder.DropTable(
                name: "registros_auditoria");
        }
    }
}
