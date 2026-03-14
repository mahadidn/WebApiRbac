using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApiRbac.Migrations
{
    /// <inheritdoc />
    public partial class AddReplacedByTokenForReuseDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReplacedByToken",
                table: "refresh_tokens",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplacedByToken",
                table: "refresh_tokens");
        }
    }
}
