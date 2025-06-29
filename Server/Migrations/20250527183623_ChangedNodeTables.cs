using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangedNodeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropTable(
                name: "UserSetupChannels");

            migrationBuilder.DropTable(
                name: "UserSetupFilters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetupLinks",
                table: "UserSetupLinks");

            migrationBuilder.DropIndex(
                name: "IX_UserSetupLinks_ChannelSourceId",
                table: "UserSetupLinks");

            migrationBuilder.DropIndex(
                name: "IX_UserSetupLinks_FilterSourceId",
                table: "UserSetupLinks");

            migrationBuilder.DropIndex(
                name: "IX_UserSetupLinks_FilterTargetId",
                table: "UserSetupLinks");

            migrationBuilder.DropIndex(
                name: "IX_UserSetupLinks_VehicleFunctionTargetId",
                table: "UserSetupLinks");

            migrationBuilder.DropColumn(
                name: "ChannelSourceId",
                table: "UserSetupLinks");

            migrationBuilder.DropColumn(
                name: "FilterSourceId",
                table: "UserSetupLinks");

            migrationBuilder.DropColumn(
                name: "FilterTargetId",
                table: "UserSetupLinks");

            migrationBuilder.DropColumn(
                name: "VehicleFunctionTargetId",
                table: "UserSetupLinks");

            migrationBuilder.RenameTable(
                name: "UserSetupLinks",
                newName: "UserSetupLink");

            migrationBuilder.RenameColumn(
                name: "UserSetupId",
                table: "UserSetupLink",
                newName: "UserSetupToNodeId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLinks_UserSetupId",
                table: "UserSetupLink",
                newName: "IX_UserSetupLink_UserSetupToNodeId");

            migrationBuilder.AddColumn<int>(
                name: "UserSetupFromNodeId",
                table: "UserSetupLink",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetupLink",
                table: "UserSetupLink",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UserChannelDevice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserDeviceName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChannelDevice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserChannel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserChannelDeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAxis = table.Column<bool>(type: "INTEGER", nullable: false),
                    CalibrationMin = table.Column<float>(type: "REAL", nullable: false),
                    CalibrationMax = table.Column<float>(type: "REAL", nullable: false),
                    PositionX = table.Column<float>(type: "REAL", nullable: false),
                    PositionY = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChannel_UserChannelDevice_UserChannelDeviceId",
                        column: x => x.UserChannelDeviceId,
                        principalTable: "UserChannelDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupFlowNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionX = table.Column<float>(type: "REAL", nullable: false),
                    PositionY = table.Column<float>(type: "REAL", nullable: false),
                    NodeType = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    CarChannelId = table.Column<int>(type: "INTEGER", nullable: true),
                    SetupFunctionId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserChannelId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupFlowNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupFlowNodes_CarChannels_CarChannelId",
                        column: x => x.CarChannelId,
                        principalTable: "CarChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupFlowNodes_UserChannel_UserChannelId",
                        column: x => x.UserChannelId,
                        principalTable: "UserChannel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupFlowNodes_UserSetups_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLink_UserSetupFromNodeId",
                table: "UserSetupLink",
                column: "UserSetupFromNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChannel_UserChannelDeviceId",
                table: "UserChannel",
                column: "UserChannelDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFlowNodes_CarChannelId",
                table: "UserSetupFlowNodes",
                column: "CarChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFlowNodes_UserChannelId",
                table: "UserSetupFlowNodes",
                column: "UserChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFlowNodes_UserSetupId",
                table: "UserSetupFlowNodes",
                column: "UserSetupId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_UserSetupFlowNodes_UserSetupFromNodeId",
                table: "UserSetupLink",
                column: "UserSetupFromNodeId",
                principalTable: "UserSetupFlowNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSetupLink_UserSetupFlowNodes_UserSetupToNodeId",
                table: "UserSetupLink",
                column: "UserSetupToNodeId",
                principalTable: "UserSetupFlowNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_UserSetupFlowNodes_UserSetupFromNodeId",
                table: "UserSetupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSetupLink_UserSetupFlowNodes_UserSetupToNodeId",
                table: "UserSetupLink");

            migrationBuilder.DropTable(
                name: "UserSetupFlowNodes");

            migrationBuilder.DropTable(
                name: "UserChannel");

            migrationBuilder.DropTable(
                name: "UserChannelDevice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSetupLink",
                table: "UserSetupLink");

            migrationBuilder.DropIndex(
                name: "IX_UserSetupLink_UserSetupFromNodeId",
                table: "UserSetupLink");

            migrationBuilder.DropColumn(
                name: "UserSetupFromNodeId",
                table: "UserSetupLink");

            migrationBuilder.RenameTable(
                name: "UserSetupLink",
                newName: "UserSetupLinks");

            migrationBuilder.RenameColumn(
                name: "UserSetupToNodeId",
                table: "UserSetupLinks",
                newName: "UserSetupId");

            migrationBuilder.RenameIndex(
                name: "IX_UserSetupLink_UserSetupToNodeId",
                table: "UserSetupLinks",
                newName: "IX_UserSetupLinks_UserSetupId");

            migrationBuilder.AddColumn<int>(
                name: "ChannelSourceId",
                table: "UserSetupLinks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FilterSourceId",
                table: "UserSetupLinks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FilterTargetId",
                table: "UserSetupLinks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleFunctionTargetId",
                table: "UserSetupLinks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSetupLinks",
                table: "UserSetupLinks",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UserSetupChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    CalibrationMax = table.Column<float>(type: "REAL", nullable: false),
                    CalibrationMin = table.Column<float>(type: "REAL", nullable: false),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAxis = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    PositionX = table.Column<float>(type: "REAL", nullable: false),
                    PositionY = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupChannels_UserSetups_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSetupFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SetupFilterTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Paramerters = table.Column<string>(type: "TEXT", nullable: true),
                    PositionX = table.Column<float>(type: "REAL", nullable: false),
                    PositionY = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetupFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetupFilters_SetupFilterTypes_SetupFilterTypeId",
                        column: x => x.SetupFilterTypeId,
                        principalTable: "SetupFilterTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetupFilters_UserSetups_UserSetupId",
                        column: x => x.UserSetupId,
                        principalTable: "UserSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLinks_ChannelSourceId",
                table: "UserSetupLinks",
                column: "ChannelSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLinks_FilterSourceId",
                table: "UserSetupLinks",
                column: "FilterSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLinks_FilterTargetId",
                table: "UserSetupLinks",
                column: "FilterTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupLinks_VehicleFunctionTargetId",
                table: "UserSetupLinks",
                column: "VehicleFunctionTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupChannels_UserSetupId",
                table: "UserSetupChannels",
                column: "UserSetupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFilters_SetupFilterTypeId",
                table: "UserSetupFilters",
                column: "SetupFilterTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetupFilters_UserSetupId",
                table: "UserSetupFilters",
                column: "UserSetupId");

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
        }
    }
}
