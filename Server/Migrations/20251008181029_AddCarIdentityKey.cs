using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCarIdentityKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CarId",
                table: "Cars",
                newName: "CarIdentityKey");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_CarIdentityKey",
                table: "Cars",
                column: "CarIdentityKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cars_CarIdentityKey",
                table: "Cars");

            migrationBuilder.RenameColumn(
                name: "CarIdentityKey",
                table: "Cars",
                newName: "CarId");
        }
    }
}
