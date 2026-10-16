using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "guest",
                columns: table => new
                {
                    guest_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    passport_number = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest", x => x.guest_id);
                });

            migrationBuilder.CreateTable(
                name: "room",
                columns: table => new
                {
                    room_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    floor = table.Column<int>(type: "integer", nullable: false),
                    size = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    housekeeping_status = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room", x => x.room_id);
                    table.CheckConstraint("ck_room_capacity_positive", "capacity > 0");
                    table.CheckConstraint("ck_room_floor_positive", "floor > 0");
                });

            migrationBuilder.CreateTable(
                name: "booking",
                columns: table => new
                {
                    booking_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    check_in_time = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    check_out_time = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    check_out_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    room_id = table.Column<int>(type: "integer", nullable: false),
                    guest_id = table.Column<int>(type: "integer", nullable: false),
                    effective_end_date = table.Column<DateOnly>(type: "date", nullable: false, computedColumnSql: "GREATEST(start_date + 1, COALESCE(check_out_date, end_date))", stored: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking", x => x.booking_id);
                    table.CheckConstraint("ck_booking_end_after_start", "end_date > start_date");
                    table.CheckConstraint("ck_booking_status_range", "status BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "fk_booking_guest_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guest",
                        principalColumn: "guest_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_booking_room_room_id",
                        column: x => x.room_id,
                        principalTable: "room",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_guest_id",
                table: "booking",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_room_id",
                table: "booking",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_status_start_date",
                table: "booking",
                columns: new[] { "status", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_last_name_first_name",
                table: "guest",
                columns: new[] { "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "ix_room_status",
                table: "room",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_room_room_number",
                table: "room",
                column: "room_number",
                unique: true);

            // --------------------------------------------------------
            // EXCLUSION CONSTRAINT (A-01, A-02, B-03)
            // --------------------------------------------------------
            // EF Core kan ikke modellere en exclusion constraint, så den skrives som rå SQL.
            // Definitionen står i BookingOverlapConstraint og udledes af BookingStatusExtensions og Booking i
            // Domain (A-04) — den må ikke skrives af i hånden her, for så kan SQL og C#
            // blive uenige om hvilke statusser der spærrer.
            //
            // btree_gist-extensionen oprettes allerede af AlterDatabase-annotationen øverst;
            // constrainten kræver den for at kunne blande '=' på room_id med '&&' på daterange.
            //
            // Rækkefølgen er vigtig: den genererede kolonne effective_end_date skal findes,
            // før den kan indgå i constrainten. Den oprettes af CreateTable ovenfor.
            migrationBuilder.Sql(BookingOverlapConstraint.CreateConstraintSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BookingOverlapConstraint.DropConstraintSql);

            migrationBuilder.DropTable(
                name: "booking");

            migrationBuilder.DropTable(
                name: "guest");

            migrationBuilder.DropTable(
                name: "room");
        }
    }
}