using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketAnalysisRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "market_analysis_run",
                columns: table => new
                {
                    market_analysis_run_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    content_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_market_analysis_run", x => x.market_analysis_run_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_market_analysis_run_finished_at",
                table: "market_analysis_run",
                column: "finished_at");

            migrationBuilder.CreateIndex(
                name: "ux_market_analysis_run_run_id",
                table: "market_analysis_run",
                column: "run_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "market_analysis_run");
        }
    }
}
