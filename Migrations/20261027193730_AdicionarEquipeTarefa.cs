using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GFGestao.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarEquipeTarefa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Equipe",
                table: "Tarefas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Equipe",
                table: "Tarefas");
        }
    }
}
