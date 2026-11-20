using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Persistence.Converters;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="BookingRequest"/> (MIL-088, ADR-0052 decisions 2, 6 and 7): a booking asked for on the
/// public site and not yet verified. The table keeps the SHA-256 of the token and of the mailbox, each under a unique
/// index, never the token or the mailbox, and the passport number encrypted with the key of the guest's.
/// </summary>
/// <remarks>
/// The class has a dependency and is therefore registered explicitly in <see cref="HotelDbContext.OnModelCreating"/>,
/// as <see cref="GuestConfiguration"/> is.
/// </remarks>
public sealed class BookingRequestConfiguration : EntityConfiguration<BookingRequest, int>
{
    #region Fields

    private readonly IStringEncryptor _passportEncryptor;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the configuration.</summary>
    /// <param name="passportEncryptor">The encryption of the passport number, the one the guest's uses.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="passportEncryptor"/> is <c>null</c>.</exception>
    public BookingRequestConfiguration(IStringEncryptor passportEncryptor)
    {
        ArgumentNullException.ThrowIfNull(passportEncryptor);

        _passportEncryptor = passportEncryptor;
    }

    #endregion Constructors

    #region Properties

    /// <inheritdoc />
    protected override string TableName => "booking_request";

    #endregion Properties

    #region Protected methods

    /// <inheritdoc />
    protected override void ConfigureTable(TableBuilder<BookingRequest> table)
    {
        ArgumentNullException.ThrowIfNull(table);

        table.HasCheckConstraint("ck_booking_request_period", "end_date > start_date");
        table.HasCheckConstraint("ck_booking_request_link", "expires_at > created_at");
    }

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<BookingRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(request => request.TokenHash)
            .HasMaxLength(BookingRequest.HashLength)
            .IsRequired();

        builder.Property(request => request.EmailKey)
            .HasMaxLength(BookingRequest.HashLength)
            .IsRequired();

        builder.Property(request => request.RoomSize)
            .HasConversion<int>()
            .IsRequired();

        // The fields of the guest have the lengths of the guest's, so a request holds what a guest may hold.
        builder.Property(request => request.FirstName)
            .HasMaxLength(Guest.MaxNameLength)
            .IsRequired();

        builder.Property(request => request.LastName)
            .HasMaxLength(Guest.MaxNameLength)
            .IsRequired();

        builder.Property(request => request.Email)
            .HasMaxLength(Guest.MaxEmailLength)
            .IsRequired();

        builder.Property(request => request.PhoneNumber)
            .HasMaxLength(Guest.MaxPhoneNumberLength)
            .IsRequired();

        builder.Property(request => request.Country)
            .HasMaxLength(Guest.MaxCountryLength)
            .IsRequired();

        // Encrypted at rest as the passport number of a guest is (B-09); see GuestConfiguration for the cast and NULL.
        builder.Property(request => request.PassportNumber)
            .HasConversion((ValueConverter)new EncryptedStringConverter(_passportEncryptor))
            .HasColumnType("text");

        // One statement claims a request by its hash (decision 5); one open request per mailbox (decision 6).
        builder.HasIndex(request => request.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_booking_request_token_hash");

        builder.HasIndex(request => request.EmailKey)
            .IsUnique()
            .HasDatabaseName("ux_booking_request_email_key");

        // The clean-up deletes the requests whose link has lapsed (decision 7).
        builder.HasIndex(request => request.ExpiresAt)
            .HasDatabaseName("ix_booking_request_expires_at");

        builder.Ignore(request => request.Period);

        builder.UseXminAsConcurrencyToken();
    }

    #endregion Protected methods
}
