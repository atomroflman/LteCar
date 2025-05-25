using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSetupFilterToDbSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFilters_SetupFilterType_SetupFilterTypeId",
                table: "UserSetupFilters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SetupFilterType",
                table: "SetupFilterType");

            migrationBuilder.RenameTable(
                name: "SetupFilterType",
                newName: "SetupFilterTypes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SetupFilterTypes",
                table: "SetupFilterTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFilters_SetupFilterTypes_SetupFilterTypeId",
                table: "UserSetupFilters",
                column: "SetupFilterTypeId",
                principalTable: "SetupFilterTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFilters_SetupFilterTypes_SetupFilterTypeId",
                table: "UserSetupFilters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SetupFilterTypes",
                table: "SetupFilterTypes");

            migrationBuilder.RenameTable(
                name: "SetupFilterTypes",
                newName: "SetupFilterType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SetupFilterType",
                table: "SetupFilterType",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFilters_SetupFilterType_SetupFilterTypeId",
                table: "UserSetupFilters",
                column: "SetupFilterTypeId",
                principalTable: "SetupFilterType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
