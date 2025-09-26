using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProcessIdFromCarVideoStreams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessId",
                table: "CarVideoStreams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcessId",
                table: "CarVideoStreams",
                type: "INTEGER",
                nullable: true);
        }
    }
}
