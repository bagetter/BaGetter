using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaGetter.Database.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiFeedForeignKeys : Migration
    {
        // Split out from AddMultiFeedSupport: SQLite table rebuild for AddForeignKey
        // toggles PRAGMA foreign_keys and therefore cannot run inside a transaction.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_FeedPermissions_Feeds_FeedId",
                table: "FeedPermissions",
                column: "FeedId",
                principalTable: "Feeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_Feeds_FeedId",
                table: "Packages",
                column: "FeedId",
                principalTable: "Feeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeedPermissions_Feeds_FeedId",
                table: "FeedPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Packages_Feeds_FeedId",
                table: "Packages");
        }
    }
}
