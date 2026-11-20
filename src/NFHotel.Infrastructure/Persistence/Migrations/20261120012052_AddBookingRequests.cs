using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_request",
                columns: table => new
                {
                    booking_request_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    room_size = table.Column<int>(type: "integer", nullable: false),
                    guests = table.Column<int>(type: "integer", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    passport_number = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_request", x => x.booking_request_id);
                    table.CheckConstraint("ck_booking_request_link", "expires_at > created_at");
                    table.CheckConstraint("ck_booking_request_period", "end_date > start_date");
                });

            migrationBuilder.CreateTable(
                name: "sent_mail",
                columns: table => new
                {
                    sent_mail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sent_mail", x => x.sent_mail_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_request_expires_at",
                table: "booking_request",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ux_booking_request_email_key",
                table: "booking_request",
                column: "email_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_booking_request_token_hash",
                table: "booking_request",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sent_mail_email_key_kind_sent_at",
                table: "sent_mail",
                columns: new[] { "email_key", "kind", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sent_mail_sent_at",
                table: "sent_mail",
                column: "sent_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_request");

            migrationBuilder.DropTable(
                name: "sent_mail");
        }
    }
}
