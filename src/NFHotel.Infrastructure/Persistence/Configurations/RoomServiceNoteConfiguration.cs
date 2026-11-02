using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="RoomServiceNote"/> to the table <c>room_service_note</c>.</summary>
public sealed class RoomServiceNoteConfiguration : EntityConfiguration<RoomServiceNote, int>
{
    /// <inheritdoc />
    protected override string TableName => "room_service_note";

    /// <inheritdoc />
    protected override void ConfigureTable(TableBuilder<RoomServiceNote> table) =>
        table.HasCheckConstraint(
            "ck_room_service_note_done_fields_together",
            "(done_by IS NULL) = (done_at IS NULL)");

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<RoomServiceNote> builder)
    {
        builder.Property(note => note.Text).HasMaxLength(RoomServiceNote.MaxTextLength).IsRequired();
        builder.Property(note => note.WrittenBy).HasMaxLength(RoomServiceNote.MaxNameLength).IsRequired();
        builder.Property(note => note.WrittenAt).IsRequired();
        builder.Property(note => note.DoneBy).HasMaxLength(RoomServiceNote.MaxNameLength);

        // Deleting a room removes it for good, and its notes say nothing without it.
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(note => note.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // The history of one room, and the notes of every room that are not in the history.
        builder.HasIndex(note => new { note.RoomId, note.DoneAt })
            .HasDatabaseName("ix_room_service_note_room_done_at");
        builder.HasIndex(note => note.DoneAt)
            .HasDatabaseName("ix_room_service_note_done_at");

        builder.Ignore(note => note.UndoableUntil);
    }
}
