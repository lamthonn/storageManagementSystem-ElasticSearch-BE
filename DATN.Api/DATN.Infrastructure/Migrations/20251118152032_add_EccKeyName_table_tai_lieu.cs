using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_EccKeyName_table_tai_lieu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EccKeyName",
                table: "tai_lieu",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EccKeyName",
                table: "tai_lieu");
        }
    }
}
