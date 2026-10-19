using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Persistence.Converters;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="NFHotel.Domain.Entities.Guest"/>, including encryption of the passport number (B-09, D-04).
/// </summary>
/// <remarks>
/// The class has a dependency and is therefore registered explicitly in
/// <see cref="HotelDbContext.OnModelCreating"/> rather than via assembly scanning.
/// </remarks>
public sealed class GuestConfiguration : EntityConfiguration<Guest, int>
{
    private readonly IStringEncryptor _passportEncryptor;

    /// <summary>
    /// Creates the configuration.
    /// </summary>
    /// <param name="passportEncryptor">Krypteringstjenesten for pasnummer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="passportEncryptor"/> is <c>null</c>.
    /// </exception>
    public GuestConfiguration(IStringEncryptor passportEncryptor)
    {
        ArgumentNullException.ThrowIfNull(passportEncryptor);

        _passportEncryptor = passportEncryptor;
    }

    /// <inheritdoc />
    protected override string TableName => "guest";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Guest> builder)
    {
        // The lengths mirror the constants of the domain (A-10). Otherwise a value that is too long
        // would fail as a DbUpdateException instead of as a domain error.
        builder.Property(guest => guest.FirstName)
            .HasMaxLength(Guest.MaxNameLength)
            .IsRequired();

        builder.Property(guest => guest.LastName)
            .HasMaxLength(Guest.MaxNameLength)
            .IsRequired();

        builder.Property(guest => guest.Email)
            .HasMaxLength(Guest.MaxEmailLength)
            .IsRequired();

        builder.Property(guest => guest.PhoneNumber)
            .HasMaxLength(Guest.MaxPhoneNumberLength)
            .IsRequired();

        builder.Property(guest => guest.Country)
            .HasMaxLength(Guest.MaxCountryLength)
            .IsRequired();

        // B-09: encrypted at rest. The column type is text and not varchar(50), because
        // nonce + tag + ciphertext in base64 take up considerably more than the plain text.
        // The length limit (A-10) is enforced by the attributes of Guest on the plain text, not here.
        // The cast to the non-generic ValueConverter is necessary: the property is
        // string?, the converter string→string. EF never calls the converter for null, so
        // NULL stays NULL in the database (T-41).
        builder.Property(guest => guest.PassportNumber)
            .HasConversion((ValueConverter)new EncryptedStringConverter(_passportEncryptor))
            .HasColumnType("text");

        // BR-68 and BR-69: guest search and sorting. Btree is plenty at this
        // data volume; if ILIKE '%x%' becomes slow, a GIN/pg_trgm index is the next step.
        builder.HasIndex(guest => new { guest.LastName, guest.FirstName })
            .HasDatabaseName("ix_guest_last_name_first_name");

        // DELIBERATELY NO INDEX ON passport_number. AES-GCM is randomised, so the same
        // passport number gives different ciphertext every time — even an equality lookup would
        // not work. None of the 126 ported rules searches or sorts on the field.
        builder.Ignore(guest => guest.FullName);

        builder.UseXminAsConcurrencyToken();
    }
}
