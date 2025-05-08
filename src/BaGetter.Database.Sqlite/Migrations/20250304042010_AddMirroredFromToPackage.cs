using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaGetter.Database.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMirroredFromToPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MirroredFrom",
                table: "Packages",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MirroredFrom",
                table: "Packages");
        }
    }
}
