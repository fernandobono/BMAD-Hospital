using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalTurnos.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class UnaPropuestaEditablePorMedico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_agendas_medico_una_editable",
                table: "agendas_medico",
                column: "medico_id",
                unique: true,
                filter: "estado IN ('Borrador', 'Rechazada')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_agendas_medico_una_editable",
                table: "agendas_medico");
        }
    }
}
