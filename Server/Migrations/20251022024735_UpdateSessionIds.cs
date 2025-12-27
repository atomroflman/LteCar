using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSessionIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_SessionToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SessionToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TransferCodeExpiresAt",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "TransferCodeExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TransferCode",
                table: "Users",
                type: "bigint",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SessionId",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SessionId",
                table: "Users",
                column: "SessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_SessionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "TransferCodeExpiresAt",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TransferCode",
                table: "Users",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionToken",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SessionToken",
                table: "Users",
                column: "SessionToken",
                unique: true,
                filter: "[SessionToken] IS NOT NULL");
        }
    }
}
