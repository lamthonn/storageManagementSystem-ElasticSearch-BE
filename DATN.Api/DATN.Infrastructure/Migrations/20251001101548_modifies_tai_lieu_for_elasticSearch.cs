using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modifies_tai_lieu_for_elasticSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentText",
                table: "tai_lieu",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "tai_lieu",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "tai_lieu",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "tai_lieu",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IndexStatus",
                table: "tai_lieu",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "IndexedAt",
                table: "tai_lieu",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "tai_lieu",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "isPublic",
                table: "tai_lieu",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentText",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "IndexStatus",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "IndexedAt",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "tai_lieu");

            migrationBuilder.DropColumn(
                name: "isPublic",
                table: "tai_lieu");
        }
    }
}
