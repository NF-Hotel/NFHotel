using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomServiceNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_service_note",
                columns: table => new
                {
                    room_service_note_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    written_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    written_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    done_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    done_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_service_note", x => x.room_service_note_id);
                    table.CheckConstraint("ck_room_service_note_done_fields_together", "(done_by IS NULL) = (done_at IS NULL)");
                    table.ForeignKey(
                        name: "fk_room_service_note_room_room_id",
                        column: x => x.room_id,
                        principalTable: "room",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_room_service_note_done_at",
                table: "room_service_note",
                column: "done_at");

            migrationBuilder.CreateIndex(
                name: "ix_room_service_note_room_done_at",
                table: "room_service_note",
                columns: new[] { "room_id", "done_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_service_note");
        }
    }
}
