using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomRateAndAuditDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "detail",
                table: "staff_audit_event",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "room_rate",
                columns: table => new
                {
                    room_rate_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    size = table.Column<int>(type: "integer", nullable: false),
                    nightly_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    set_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    set_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_rate", x => x.room_rate_id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_room_rate_size",
                table: "room_rate",
                column: "size",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_rate");

            migrationBuilder.DropColumn(
                name: "detail",
                table: "staff_audit_event");
        }
    }
}
