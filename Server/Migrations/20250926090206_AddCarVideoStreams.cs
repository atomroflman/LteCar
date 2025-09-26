using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCarVideoStreams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarVideoStreams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StreamId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    Protocol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    ProcessArguments = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ProcessId = table.Column<int>(type: "INTEGER", nullable: true),
                    StreamPurpose = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarVideoStreams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarVideoStreams_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_CarId_IsActive",
                table: "CarVideoStreams",
                columns: new[] { "CarId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_Port",
                table: "CarVideoStreams",
                column: "Port");

            migrationBuilder.CreateIndex(
                name: "IX_CarVideoStreams_StreamId",
                table: "CarVideoStreams",
                column: "StreamId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarVideoStreams");
        }
    }
}
