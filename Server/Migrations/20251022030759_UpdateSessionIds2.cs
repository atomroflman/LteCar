using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LteCar.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSessionIds2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "UserSessionSeq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "UserSessionSeq");
        }
    }
}
