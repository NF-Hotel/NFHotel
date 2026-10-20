using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "benchmark_run",
                columns: table => new
                {
                    benchmark_run_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dataset_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dataset_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    dataset_rows = table.Column<int>(type: "integer", nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cpu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cpu_cores = table.Column<int>(type: "integer", nullable: false),
                    memory_mb = table.Column<int>(type: "integer", nullable: false),
                    gpu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    operating_system = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    backend_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    backend_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_run", x => x.benchmark_run_id);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_result",
                columns: table => new
                {
                    benchmark_result_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    benchmark_run_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    backend = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    analysis_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    task_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    run_number = table.Column<int>(type: "integer", nullable: false),
                    cold = table.Column<bool>(type: "boolean", nullable: false),
                    time_to_first_token_seconds = table.Column<double>(type: "double precision", nullable: true),
                    total_seconds = table.Column<double>(type: "double precision", nullable: false),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: true),
                    tokens_per_second = table.Column<double>(type: "double precision", nullable: true),
                    tokens_estimated = table.Column<bool>(type: "boolean", nullable: false),
                    finish_reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ram_before_mb = table.Column<double>(type: "double precision", nullable: true),
                    ram_after_mb = table.Column<double>(type: "double precision", nullable: true),
                    ram_peak_mb = table.Column<double>(type: "double precision", nullable: true),
                    vram_before_mb = table.Column<double>(type: "double precision", nullable: true),
                    vram_after_mb = table.Column<double>(type: "double precision", nullable: true),
                    vram_peak_mb = table.Column<double>(type: "double precision", nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: true),
                    response_valid = table.Column<bool>(type: "boolean", nullable: true),
                    validation_error = table.Column<string>(type: "text", maxLength: 1000000, nullable: true),
                    output = table.Column<string>(type: "text", maxLength: 1000000, nullable: true),
                    error = table.Column<string>(type: "text", maxLength: 1000000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_result", x => x.benchmark_result_id);
                    table.CheckConstraint("ck_benchmark_result_completion_tokens_non_negative", "completion_tokens >= 0");
                    table.CheckConstraint("ck_benchmark_result_total_seconds_non_negative", "total_seconds >= 0");
                    table.ForeignKey(
                        name: "fk_benchmark_result_benchmark_run_benchmark_run_id",
                        column: x => x.benchmark_run_id,
                        principalTable: "benchmark_run",
                        principalColumn: "benchmark_run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_result_benchmark_run_id",
                table: "benchmark_result",
                column: "benchmark_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_run_started_at",
                table: "benchmark_run",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ux_benchmark_run_run_id",
                table: "benchmark_run",
                column: "run_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "benchmark_result");

            migrationBuilder.DropTable(
                name: "benchmark_run");
        }
    }
}
