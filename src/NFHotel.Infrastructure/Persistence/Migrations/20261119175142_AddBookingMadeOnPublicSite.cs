using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingMadeOnPublicSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "made_on_public_site",
                table: "booking",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "made_on_public_site",
                table: "booking");
        }
    }
}
