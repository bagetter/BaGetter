using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaGetter.Database.MySql.Migrations;

/// <inheritdoc />
public partial class UseCharSetLatin1 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterTable(
            name: "TargetFrameworks")
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterTable(
            name: "PackageTypes")
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterTable(
            name: "Packages")
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterTable(
            name: "PackageDependencies")
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Moniker",
            table: "TargetFrameworks",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Version",
            table: "PackageTypes",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(64)",
            oldMaxLength: 64,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "PackageTypes",
            type: "varchar(512)",
            maxLength: 512,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(512)",
            oldMaxLength: 512,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Version",
            table: "Packages",
            type: "varchar(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(64)",
            oldMaxLength: 64)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Title",
            table: "Packages",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Tags",
            table: "Packages",
            type: "varchar(4000)",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Summary",
            table: "Packages",
            type: "varchar(4000)",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<DateTime>(
            name: "RowVersion",
            table: "Packages",
            type: "timestamp(6)",
            rowVersion: true,
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp(6)",
            oldRowVersion: true,
            oldNullable: true)
            .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

        migrationBuilder.AlterColumn<string>(
            name: "RepositoryUrl",
            table: "Packages",
            type: "varchar(4000)",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "RepositoryType",
            table: "Packages",
            type: "varchar(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(100)",
            oldMaxLength: 100,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "ReleaseNotes",
            table: "Packages",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "ProjectUrl",
            table: "Packages",
            type: "varchar(4000)",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "OriginalVersion",
            table: "Packages",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(64)",
            oldMaxLength: 64,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "MinClientVersion",
            table: "Packages",
            type: "varchar(44)",
            maxLength: 44,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(44)",
            oldMaxLength: 44,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "LicenseUrl",
            table: "Packages",
            type: "varchar(4000)",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Language",
            table: "Packages",
            type: "varchar(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(20)",
            oldMaxLength: 20,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Id",
            table: "Packages",
            type: "varchar(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(128)",
            oldMaxLength: 128)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "IconUrl",
            table: "Packages",
            type: "varchar(4000)",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Packages",
            type: "varchar(4000)",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Authors",
            table: "Packages",
            type: "varchar(4000)",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext CHARACTER SET utf8mb4",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "VersionRange",
            table: "PackageDependencies",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "TargetFramework",
            table: "PackageDependencies",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Id",
            table: "PackageDependencies",
            type: "varchar(128)",
            maxLength: 128,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(128)",
            oldMaxLength: 128,
            oldNullable: true)
            .Annotation("MySql:CharSet", "latin1")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterTable(
            name: "TargetFrameworks")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterTable(
            name: "PackageTypes")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterTable(
            name: "Packages")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterTable(
            name: "PackageDependencies")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Moniker",
            table: "TargetFrameworks",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Version",
            table: "PackageTypes",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(64)",
            oldMaxLength: 64,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "PackageTypes",
            type: "varchar(512)",
            maxLength: 512,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(512)",
            oldMaxLength: 512,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Version",
            table: "Packages",
            type: "varchar(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(64)",
            oldMaxLength: 64)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Title",
            table: "Packages",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Tags",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(4000)",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Summary",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(4000)",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<DateTime>(
            name: "RowVersion",
            table: "Packages",
            type: "timestamp(6)",
            rowVersion: true,
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp(6)",
            oldRowVersion: true,
            oldNullable: true)
            .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

        migrationBuilder.AlterColumn<string>(
            name: "RepositoryUrl",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(4000)",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "RepositoryType",
            table: "Packages",
            type: "varchar(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(100)",
            oldMaxLength: 100,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "ReleaseNotes",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "ProjectUrl",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(4000)",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "OriginalVersion",
            table: "Packages",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(64)",
            oldMaxLength: 64,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "MinClientVersion",
            table: "Packages",
            type: "varchar(44)",
            maxLength: 44,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(44)",
            oldMaxLength: 44,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "LicenseUrl",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(4000)",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Language",
            table: "Packages",
            type: "varchar(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(20)",
            oldMaxLength: 20,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Id",
            table: "Packages",
            type: "varchar(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(128)",
            oldMaxLength: 128)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "IconUrl",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(4000)",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(4000)",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Authors",
            table: "Packages",
            type: "longtext CHARACTER SET utf8mb4",
            maxLength: 4000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(4000)",
            oldMaxLength: 4000,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "VersionRange",
            table: "PackageDependencies",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "TargetFramework",
            table: "PackageDependencies",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");

        migrationBuilder.AlterColumn<string>(
            name: "Id",
            table: "PackageDependencies",
            type: "varchar(128)",
            maxLength: 128,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(128)",
            oldMaxLength: 128,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "latin1");
    }
}
