using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffNameAndEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "staff_member",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "staff_member",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "staff_member",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_staff_member_email",
                table: "staff_member",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_staff_member_email",
                table: "staff_member");

            migrationBuilder.DropColumn(
                name: "email",
                table: "staff_member");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "staff_member");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "staff_member");
        }
    }
}
