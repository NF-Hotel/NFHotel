using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NFHotel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffPasswordAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "must_change_password",
                table: "staff_member",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "security_stamp",
                table: "staff_member",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Passwords that existed before this migration were never rotated, and the first Manager password was
            // seen in a chat (MIL-030): every existing account chooses a new password at its next sign-in.
            migrationBuilder.Sql("UPDATE staff_member SET must_change_password = TRUE;");

            migrationBuilder.CreateTable(
                name: "staff_audit_event",
                columns: table => new
                {
                    staff_audit_event_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<int>(type: "integer", nullable: true),
                    actor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    action = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<int>(type: "integer", nullable: true),
                    target_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outcome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    remote_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_audit_event", x => x.staff_audit_event_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staff_audit_event_occurred_at",
                table: "staff_audit_event",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_audit_event");

            migrationBuilder.DropColumn(
                name: "must_change_password",
                table: "staff_member");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                table: "staff_member");
        }
    }
}
