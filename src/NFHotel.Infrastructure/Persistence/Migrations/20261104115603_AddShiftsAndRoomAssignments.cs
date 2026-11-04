using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftsAndRoomAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_assignment",
                columns: table => new
                {
                    room_assignment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<int>(type: "integer", nullable: false),
                    staff_member_id = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_assignment", x => x.room_assignment_id);
                    table.ForeignKey(
                        name: "fk_room_assignment_room_room_id",
                        column: x => x.room_id,
                        principalTable: "room",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_room_assignment_staff_members_staff_member_id",
                        column: x => x.staff_member_id,
                        principalTable: "staff_member",
                        principalColumn: "staff_member_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_trade",
                columns: table => new
                {
                    room_trade_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    from_staff_member_id = table.Column<int>(type: "integer", nullable: false),
                    to_staff_member_id = table.Column<int>(type: "integer", nullable: false),
                    give_room_ids = table.Column<int[]>(type: "integer[]", nullable: false),
                    take_room_ids = table.Column<int[]>(type: "integer[]", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    offered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by_id = table.Column<int>(type: "integer", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_trade", x => x.room_trade_id);
                    table.ForeignKey(
                        name: "fk_room_trade_staff_members_from_staff_member_id",
                        column: x => x.from_staff_member_id,
                        principalTable: "staff_member",
                        principalColumn: "staff_member_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_room_trade_staff_members_to_staff_member_id",
                        column: x => x.to_staff_member_id,
                        principalTable: "staff_member",
                        principalColumn: "staff_member_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift",
                columns: table => new
                {
                    shift_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    assignee_staff_member_id = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift", x => x.shift_id);
                    table.CheckConstraint("ck_shift_ends_after_start", "ends_at > starts_at");
                    table.ForeignKey(
                        name: "fk_shift_staff_members_assignee_staff_member_id",
                        column: x => x.assignee_staff_member_id,
                        principalTable: "staff_member",
                        principalColumn: "staff_member_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "shift_trade",
                columns: table => new
                {
                    shift_trade_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shift_id = table.Column<int>(type: "integer", nullable: false),
                    offered_by_staff_member_id = table.Column<int>(type: "integer", nullable: false),
                    swap_shift_id = table.Column<int>(type: "integer", nullable: true),
                    taken_by_staff_member_id = table.Column<int>(type: "integer", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    offered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decided_by_id = table.Column<int>(type: "integer", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_trade", x => x.shift_trade_id);
                    table.ForeignKey(
                        name: "fk_shift_trade_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "shift_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shift_trade_shift_swap_shift_id",
                        column: x => x.swap_shift_id,
                        principalTable: "shift",
                        principalColumn: "shift_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shift_trade_staff_members_offered_by_staff_member_id",
                        column: x => x.offered_by_staff_member_id,
                        principalTable: "staff_member",
                        principalColumn: "staff_member_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shift_trade_staff_members_taken_by_staff_member_id",
                        column: x => x.taken_by_staff_member_id,
                        principalTable: "staff_member",
                        principalColumn: "staff_member_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_room_assignment_staff_id",
                table: "room_assignment",
                column: "staff_member_id");

            migrationBuilder.CreateIndex(
                name: "ux_room_assignment_room_id",
                table: "room_assignment",
                column: "room_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_room_trade_from_staff_id",
                table: "room_trade",
                column: "from_staff_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_room_trade_state",
                table: "room_trade",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_room_trade_to_staff_id",
                table: "room_trade",
                column: "to_staff_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_assignee_starts_at",
                table: "shift",
                columns: new[] { "assignee_staff_member_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_starts_at",
                table: "shift",
                column: "starts_at");

            migrationBuilder.CreateIndex(
                name: "ix_shift_trade_offered_by_staff_member_id",
                table: "shift_trade",
                column: "offered_by_staff_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_trade_shift_id",
                table: "shift_trade",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_trade_state_offered_at",
                table: "shift_trade",
                columns: new[] { "state", "offered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_trade_swap_shift_id",
                table: "shift_trade",
                column: "swap_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_trade_taken_by_staff_member_id",
                table: "shift_trade",
                column: "taken_by_staff_member_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_assignment");

            migrationBuilder.DropTable(
                name: "room_trade");

            migrationBuilder.DropTable(
                name: "shift_trade");

            migrationBuilder.DropTable(
                name: "shift");
        }
    }
}
