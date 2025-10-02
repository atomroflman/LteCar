using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class Sync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CarVideoStreams",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "CarVideoStreams",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStatusUpdate",
                table: "CarVideoStreams",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CarVideoStreams",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CarVideoStreams",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "CarVideoStreams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "CarVideoStreams",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "LastStatusUpdate",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "CarVideoStreams");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CarVideoStreams");
        }
    }
}
