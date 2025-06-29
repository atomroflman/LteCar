using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCarSetup_Cars_CarId",
                table: "UserCarSetup");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCarSetup_Users_UserId",
                table: "UserCarSetup");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupChannel_UserCarSetup_UserSetupId",
                table: "UserSetupChannel");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFilter_SetupFilterType_SetupFilterTypeId",
                table: "UserSetupFilter");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFilter_UserCarSetup_UserSetupId",
                table: "UserSetupFilter");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_CarChannels_VehicleFunctionTargetId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_UserCarSetup_UserSetupId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_UserSetupChannel_ChannelSourceId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_UserSetupFilter_FilterSourceId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_UserSetupFilter_FilterTargetId",
                table: "UserSetupLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetupLink",
                table: "UserSetupLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetupFilter",
                table: "UserSetupFilter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetupChannel",
                table: "UserSetupChannel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserCarSetup",
                table: "UserCarSetup");

            migrationBuilder.RenameTable(
                name: "UserSetupLink",
                newName: "UserSetupLinks");

            migrationBuilder.RenameTable(
                name: "UserSetupFilter",
                newName: "UserSetupFilters");

            migrationBuilder.RenameTable(
                name: "UserSetupChannel",
                newName: "UserSetupChannels");

            migrationBuilder.RenameTable(
                name: "UserCarSetup",
                newName: "UserSetups");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLink_VehicleFunctionTargetId",
                table: "UserSetupLinks",
                newName: "IX_UserSetupLinks_VehicleFunctionTargetId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLink_UserSetupId",
                table: "UserSetupLinks",
                newName: "IX_UserSetupLinks_UserSetupId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLink_FilterTargetId",
                table: "UserSetupLinks",
                newName: "IX_UserSetupLinks_FilterTargetId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLink_FilterSourceId",
                table: "UserSetupLinks",
                newName: "IX_UserSetupLinks_FilterSourceId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLink_ChannelSourceId",
                table: "UserSetupLinks",
                newName: "IX_UserSetupLinks_ChannelSourceId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupFilter_UserSetupId",
                table: "UserSetupFilters",
                newName: "IX_UserSetupFilters_UserSetupId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupFilter_SetupFilterTypeId",
                table: "UserSetupFilters",
                newName: "IX_UserSetupFilters_SetupFilterTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupChannel_UserSetupId",
                table: "UserSetupChannels",
                newName: "IX_UserSetupChannels_UserSetupId");

            migrationBuilder.RenameIndex(
                name: "IX_UserCarSetup_UserId",
                table: "UserSetups",
                newName: "IX_UserSetups_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserCarSetup_CarId",
                table: "UserSetups",
                newName: "IX_UserSetups_CarId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetupLinks",
                table: "UserSetupLinks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetupFilters",
                table: "UserSetupFilters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetupChannels",
                table: "UserSetupChannels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetups",
                table: "UserSetups",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CarTelemetry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelName = table.Column<string>(type: "TEXT", nullable: false),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadIntervalTicks = table.Column<int>(type: "INTEGER", nullable: false),
                    TelemetryType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarTelemetry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarTelemetry_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupTelemetries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarTelemetryId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    OverrideTicks = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupTelemetries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupTelemetries_CarTelemetry_CarTelemetryId",
                        column: x => x.CarTelemetryId,
                        principalTable: "CarTelemetry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupTelemetries_UserSetups_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarTelemetry_CarId",
                table: "CarTelemetry",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupTelemetries_CarTelemetryId",
                table: "UserSetupTelemetries",
                column: "CarTelemetryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupTelemetries_UserSetupId",
                table: "UserSetupTelemetries",
                column: "UserSetupId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupChannels_UserSetups_UserSetupId",
                table: "UserSetupChannels",
                column: "UserSetupId",
                principalTable: "UserSetups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFilters_SetupFilterType_SetupFilterTypeId",
                table: "UserSetupFilters",
                column: "SetupFilterTypeId",
                principalTable: "SetupFilterType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFilters_UserSetups_UserSetupId",
                table: "UserSetupFilters",
                column: "UserSetupId",
                principalTable: "UserSetups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLinks_CarChannels_VehicleFunctionTargetId",
                table: "UserSetupLinks",
                column: "VehicleFunctionTargetId",
                principalTable: "CarChannels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLinks_UserSetupChannels_ChannelSourceId",
                table: "UserSetupLinks",
                column: "ChannelSourceId",
                principalTable: "UserSetupChannels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLinks_UserSetupFilters_FilterSourceId",
                table: "UserSetupLinks",
                column: "FilterSourceId",
                principalTable: "UserSetupFilters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLinks_UserSetupFilters_FilterTargetId",
                table: "UserSetupLinks",
                column: "FilterTargetId",
                principalTable: "UserSetupFilters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLinks_UserSetups_UserSetupId",
                table: "UserSetupLinks",
                column: "UserSetupId",
                principalTable: "UserSetups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetups_Cars_CarId",
                table: "UserSetups",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetups_Users_UserId",
                table: "UserSetups",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupChannels_UserSetups_UserSetupId",
                table: "UserSetupChannels");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFilters_SetupFilterType_SetupFilterTypeId",
                table: "UserSetupFilters");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupFilters_UserSetups_UserSetupId",
                table: "UserSetupFilters");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLinks_CarChannels_VehicleFunctionTargetId",
                table: "UserSetupLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLinks_UserSetupChannels_ChannelSourceId",
                table: "UserSetupLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLinks_UserSetupFilters_FilterSourceId",
                table: "UserSetupLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLinks_UserSetupFilters_FilterTargetId",
                table: "UserSetupLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLinks_UserSetups_UserSetupId",
                table: "UserSetupLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetups_Cars_CarId",
                table: "UserSetups");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetups_Users_UserId",
                table: "UserSetups");

            migrationBuilder.DropTable(
                name: "UserSetupTelemetries");

            migrationBuilder.DropTable(
                name: "CarTelemetry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetups",
                table: "UserSetups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetupLinks",
                table: "UserSetupLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetupFilters",
                table: "UserSetupFilters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetupChannels",
                table: "UserSetupChannels");

            migrationBuilder.RenameTable(
                name: "UserSetups",
                newName: "UserCarSetup");

            migrationBuilder.RenameTable(
                name: "UserSetupLinks",
                newName: "UserSetupLink");

            migrationBuilder.RenameTable(
                name: "UserSetupFilters",
                newName: "UserSetupFilter");

            migrationBuilder.RenameTable(
                name: "UserSetupChannels",
                newName: "UserSetupChannel");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetups_UserId",
                table: "UserCarSetup",
                newName: "IX_UserCarSetup_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetups_CarId",
                table: "UserCarSetup",
                newName: "IX_UserCarSetup_CarId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLinks_VehicleFunctionTargetId",
                table: "UserSetupLink",
                newName: "IX_UserSetupLink_VehicleFunctionTargetId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLinks_UserSetupId",
                table: "UserSetupLink",
                newName: "IX_UserSetupLink_UserSetupId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLinks_FilterTargetId",
                table: "UserSetupLink",
                newName: "IX_UserSetupLink_FilterTargetId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLinks_FilterSourceId",
                table: "UserSetupLink",
                newName: "IX_UserSetupLink_FilterSourceId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLinks_ChannelSourceId",
                table: "UserSetupLink",
                newName: "IX_UserSetupLink_ChannelSourceId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupFilters_UserSetupId",
                table: "UserSetupFilter",
                newName: "IX_UserSetupFilter_UserSetupId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupFilters_SetupFilterTypeId",
                table: "UserSetupFilter",
                newName: "IX_UserSetupFilter_SetupFilterTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupChannels_UserSetupId",
                table: "UserSetupChannel",
                newName: "IX_UserSetupChannel_UserSetupId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserCarSetup",
                table: "UserCarSetup",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetupLink",
                table: "UserSetupLink",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetupFilter",
                table: "UserSetupFilter",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetupChannel",
                table: "UserSetupChannel",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCarSetup_Cars_CarId",
                table: "UserCarSetup",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCarSetup_Users_UserId",
                table: "UserCarSetup",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupChannel_UserCarSetup_UserSetupId",
                table: "UserSetupChannel",
                column: "UserSetupId",
                principalTable: "UserCarSetup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFilter_SetupFilterType_SetupFilterTypeId",
                table: "UserSetupFilter",
                column: "SetupFilterTypeId",
                principalTable: "SetupFilterType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupFilter_UserCarSetup_UserSetupId",
                table: "UserSetupFilter",
                column: "UserSetupId",
                principalTable: "UserCarSetup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_CarChannels_VehicleFunctionTargetId",
                table: "UserSetupLink",
                column: "VehicleFunctionTargetId",
                principalTable: "CarChannels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_UserCarSetup_UserSetupId",
                table: "UserSetupLink",
                column: "UserSetupId",
                principalTable: "UserCarSetup",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_UserSetupChannel_ChannelSourceId",
                table: "UserSetupLink",
                column: "ChannelSourceId",
                principalTable: "UserSetupChannel",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_UserSetupFilter_FilterSourceId",
                table: "UserSetupLink",
                column: "FilterSourceId",
                principalTable: "UserSetupFilter",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_UserSetupFilter_FilterTargetId",
                table: "UserSetupLink",
                column: "FilterTargetId",
                principalTable: "UserSetupFilter",
                principalColumn: "Id");
        }
    }
}
