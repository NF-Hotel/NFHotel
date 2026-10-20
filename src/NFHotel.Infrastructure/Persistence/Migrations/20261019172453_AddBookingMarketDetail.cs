using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingMarketDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_market_detail",
                columns: table => new
                {
                    booking_market_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<int>(type: "integer", nullable: false),
                    booked_at = table.Column<DateOnly>(type: "date", nullable: false),
                    price_per_night = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    adults = table.Column<int>(type: "integer", nullable: false),
                    children = table.Column<int>(type: "integer", nullable: false),
                    babies = table.Column<int>(type: "integer", nullable: false),
                    meal = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    market_segment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    customer_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    deposit_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    room_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    is_repeated_guest = table.Column<bool>(type: "boolean", nullable: false),
                    previous_cancellations = table.Column<int>(type: "integer", nullable: false),
                    booking_changes = table.Column<int>(type: "integer", nullable: false),
                    special_requests = table.Column<int>(type: "integer", nullable: false),
                    parking_spaces = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_market_detail", x => x.booking_market_detail_id);
                    table.ForeignKey(
                        name: "fk_booking_market_detail_booking_booking_id",
                        column: x => x.booking_id,
                        principalTable: "booking",
                        principalColumn: "booking_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_market_detail_booked_at",
                table: "booking_market_detail",
                column: "booked_at");

            migrationBuilder.CreateIndex(
                name: "ux_booking_market_detail_booking_id",
                table: "booking_market_detail",
                column: "booking_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_market_detail");
        }
    }
}
