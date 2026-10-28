using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="BookingMarketDetail"/> to the table <c>booking_market_detail</c> (MIL-026).</summary>
public sealed class BookingMarketDetailConfiguration : EntityConfiguration<BookingMarketDetail, int>
{
    /// <inheritdoc />
    protected override string TableName => "booking_market_detail";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<BookingMarketDetail> builder)
    {
        builder.Property(detail => detail.BookingId).IsRequired();
        builder.Property(detail => detail.BookedAt).HasColumnType("date").IsRequired();
        builder.Property(detail => detail.PricePerNight).HasColumnType("numeric(12,2)");
        builder.Property(detail => detail.Adults).IsRequired();
        builder.Property(detail => detail.Children).IsRequired();
        builder.Property(detail => detail.Babies).IsRequired();
        builder.Property(detail => detail.Meal).HasMaxLength(BookingMarketDetail.MaxTextLength);
        builder.Property(detail => detail.MarketSegment).HasMaxLength(BookingMarketDetail.MaxTextLength);
        builder.Property(detail => detail.CustomerType).HasMaxLength(BookingMarketDetail.MaxTextLength);
        builder.Property(detail => detail.DepositType).HasMaxLength(BookingMarketDetail.MaxTextLength);
        builder.Property(detail => detail.RoomType).HasMaxLength(BookingMarketDetail.MaxTextLength);
        builder.Property(detail => detail.IsRepeatedGuest).IsRequired();
        builder.Property(detail => detail.PreviousCancellations).IsRequired();
        builder.Property(detail => detail.BookingChanges).IsRequired();
        builder.Property(detail => detail.SpecialRequests).IsRequired();
        builder.Property(detail => detail.ParkingSpaces).IsRequired();

        // One row per booking at most; the details go with the booking.
        builder.HasOne<Booking>()
            .WithOne()
            .HasForeignKey<BookingMarketDetail>(detail => detail.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(detail => detail.BookingId)
            .IsUnique()
            .HasDatabaseName("ux_booking_market_detail_booking_id");

        builder.HasIndex(detail => detail.BookedAt)
            .HasDatabaseName("ix_booking_market_detail_booked_at");
    }
}
