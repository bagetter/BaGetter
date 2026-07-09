using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaGetter.Database.MySql.Migrations
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
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"),
                collation: "ascii_general_ci");

            // Data migration: map existing "default" slug references in FeedPermissions
            // to the new default feed Guid string, then remove any rows that cannot be
            // converted (unknown slugs from future multi-feed pre-release builds).
            migrationBuilder.Sql(
                "UPDATE `FeedPermissions` SET `FeedId` = '00000000-0000-0000-0000-000000000001' WHERE `FeedId` = 'default';");
            migrationBuilder.Sql(
                "DELETE FROM `FeedPermissions` WHERE `FeedId` NOT REGEXP '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';");

            migrationBuilder.AlterColumn<Guid>(
                name: "FeedId",
                table: "FeedPermissions",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .OldAnnotation("MySql:CharSet", "latin1");

            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Slug = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "latin1"),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "latin1"),
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "latin1"),
                    AllowPackageOverwrites = table.Column<int>(type: "int", nullable: true),
                    PackageDeletionBehavior = table.Column<int>(type: "int", nullable: true),
                    IsReadOnlyMode = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    MaxPackageSizeGiB = table.Column<uint>(type: "int unsigned", nullable: true),
                    RetentionMaxMajorVersions = table.Column<int>(type: "int", nullable: true),
                    RetentionMaxMinorVersions = table.Column<int>(type: "int", nullable: true),
                    RetentionMaxPatchVersions = table.Column<int>(type: "int", nullable: true),
                    RetentionMaxPrereleaseVersions = table.Column<int>(type: "int", nullable: true),
                    MirrorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MirrorPackageSource = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "latin1"),
                    MirrorLegacy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MirrorDownloadTimeoutSeconds = table.Column<int>(type: "int", nullable: true),
                    MirrorAuthType = table.Column<int>(type: "int", nullable: true),
                    MirrorAuthUsername = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "latin1"),
                    MirrorAuthPassword = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "latin1"),
                    MirrorAuthToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "latin1"),
                    MirrorAuthCustomHeaders = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "latin1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.Id);
                })
                .Annotation("MySql:CharSet", "latin1");

            // Seed the default feed row. All existing packages will get this FeedId via the
            // column default (00000000-0000-0000-0000-000000000001).
            migrationBuilder.InsertData(
                table: "Feeds",
                columns: new[] { "Id", "Slug", "Name", "Description", "MirrorEnabled", "MirrorLegacy", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), "default", "Default", null, false, false, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

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

            migrationBuilder.AlterColumn<string>(
                name: "FeedId",
                table: "FeedPermissions",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySql:CharSet", "latin1")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

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
