using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_field_phong_ban_to_table_thu_muc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "phong_ban",
                table: "thu_muc",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "phong_ban",
                table: "thu_muc");
        }
    }
}
