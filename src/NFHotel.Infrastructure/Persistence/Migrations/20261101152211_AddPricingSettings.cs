using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pricing_settings",
                columns: table => new
                {
                    pricing_settings_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slot = table.Column<int>(type: "integer", nullable: false),
                    surcharge_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    high_threshold_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    low_threshold_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    max_change_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    minimum_nightly_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    discount_sizes = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    set_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    set_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_settings", x => x.pricing_settings_id);
                    table.CheckConstraint("ck_pricing_settings_slot", "slot = 1");
                });

            migrationBuilder.CreateIndex(
                name: "ux_pricing_settings_slot",
                table: "pricing_settings",
                column: "slot",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pricing_settings");
        }
    }
}
