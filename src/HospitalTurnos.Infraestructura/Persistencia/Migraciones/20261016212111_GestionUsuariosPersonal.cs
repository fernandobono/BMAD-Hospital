using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalTurnos.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class GestionUsuariosPersonal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "debe_cambiar_contrasena",
                table: "asp_net_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "debe_cambiar_contrasena",
                table: "asp_net_users");
        }
    }
}
