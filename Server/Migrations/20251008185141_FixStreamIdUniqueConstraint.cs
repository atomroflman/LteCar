using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixStreamIdUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CarVideoStreams_StreamId",
                table: "CarVideoStreams");

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_CarId_StreamId",
                table: "CarVideoStreams",
                columns: new[] { "CarId", "StreamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CarVideoStreams_CarId_StreamId",
                table: "CarVideoStreams");

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_StreamId",
                table: "CarVideoStreams",
                column: "StreamId",
                unique: true);
        }
    }
}
