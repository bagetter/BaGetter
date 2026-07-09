using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaGetter.Database.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiFeedSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Packages_Id",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_Id_Version",
                table: "Packages");

            migrationBuilder.AddColumn<Guid>(
                name: "FeedId",
                table: "Packages",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    AllowPackageOverwrites = table.Column<int>(type: "INTEGER", nullable: true),
                    PackageDeletionBehavior = table.Column<int>(type: "INTEGER", nullable: true),
                    IsReadOnlyMode = table.Column<bool>(type: "INTEGER", nullable: true),
                    MaxPackageSizeGiB = table.Column<uint>(type: "INTEGER", nullable: true),
                    RetentionMaxMajorVersions = table.Column<int>(type: "INTEGER", nullable: true),
                    RetentionMaxMinorVersions = table.Column<int>(type: "INTEGER", nullable: true),
                    RetentionMaxPatchVersions = table.Column<int>(type: "INTEGER", nullable: true),
                    RetentionMaxPrereleaseVersions = table.Column<int>(type: "INTEGER", nullable: true),
                    MirrorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MirrorPackageSource = table.Column<string>(type: "TEXT", nullable: true),
                    MirrorLegacy = table.Column<bool>(type: "INTEGER", nullable: false),
                    MirrorDownloadTimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    MirrorAuthType = table.Column<int>(type: "INTEGER", nullable: true),
                    MirrorAuthUsername = table.Column<string>(type: "TEXT", nullable: true),
                    MirrorAuthPassword = table.Column<string>(type: "TEXT", nullable: true),
                    MirrorAuthToken = table.Column<string>(type: "TEXT", nullable: true),
                    MirrorAuthCustomHeaders = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.Id);
                });

            // Seed the default feed row. All existing packages will get this FeedId via the
            // column default (00000000-0000-0000-0000-000000000001).
            migrationBuilder.InsertData(
                table: "Feeds",
                columns: new[] { "Id", "Slug", "Name", "Description", "MirrorEnabled", "MirrorLegacy", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), "default", "Default", null, false, false, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

            // Data migration: map existing "default" slug references in FeedPermissions
            // to the new default feed Guid, then drop any rows that reference unknown feeds.
            migrationBuilder.Sql(
                "UPDATE FeedPermissions SET FeedId = '00000000-0000-0000-0000-000000000001' WHERE FeedId = 'default';");
            migrationBuilder.Sql(
                "DELETE FROM FeedPermissions WHERE FeedId NOT IN (SELECT Id FROM Feeds);");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_FeedId_Id",
                table: "Packages",
                columns: new[] { "FeedId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_FeedId_Id_Version",
                table: "Packages",
                columns: new[] { "FeedId", "Id", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_Slug",
                table: "Feeds",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feeds");

            migrationBuilder.DropIndex(
                name: "IX_Packages_FeedId_Id",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_FeedId_Id_Version",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "FeedId",
                table: "Packages");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Id",
                table: "Packages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Id_Version",
                table: "Packages",
                columns: new[] { "Id", "Version" },
                unique: true);
        }
    }
}
