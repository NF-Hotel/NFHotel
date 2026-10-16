using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Persistence.Converters;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core-mapping for <see cref="NFHotel.Domain.Entities.Guest"/>, inklusive kryptering af pasnummer (B-09, D-04).
/// </summary>
/// <remarks>
/// Klassen har en afhængighed og registreres derfor eksplicit i
/// <see cref="HotelDbContext.OnModelCreating"/> frem for via assembly-scanning.
/// </remarks>
public sealed class GuestConfiguration : EntityConfiguration<Guest, int>
{
    private readonly IStringEncryptor _passportEncryptor;

    /// <summary>
    /// Opretter konfigurationen.
    /// </summary>
    /// <param name="passportEncryptor">Krypteringstjenesten for pasnummer.</param>
    /// <exception cref="ArgumentNullException">
    /// Kastes hvis <paramref name="passportEncryptor"/> er <c>null</c>.
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
        // Længderne spejler domænets konstanter (A-10). Ellers ville en for lang værdi
        // fejle som DbUpdateException i stedet for som en domænefejl.
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

        // B-09: krypteret at-rest. Kolonnetypen er text og ikke varchar(50), fordi
        // nonce + tag + ciffertekst i base64 fylder betydeligt mere end klarteksten.
        // Længdegrænsen (A-10) håndhæves af Guest.Validate på klarteksten, ikke her.
        // Casten til den ikke-generiske ValueConverter er nødvendig: egenskaben er
        // string?, konverteren string→string. EF kalder aldrig konverteren for null, så
        // NULL forbliver NULL i databasen (T-41).
        builder.Property(guest => guest.PassportNumber)
            .HasConversion((ValueConverter)new EncryptedStringConverter(_passportEncryptor))
            .HasColumnType("text");

        // BR-68 og BR-69: gæstesøgning og -sortering. Btree er rigeligt ved dette
        // datavolumen; bliver ILIKE '%x%' langsom, er et GIN/pg_trgm-indeks næste skridt.
        builder.HasIndex(guest => new { guest.LastName, guest.FirstName })
            .HasDatabaseName("ix_guest_last_name_first_name");

        // BEVIDST INTET INDEKS PÅ passport_number. AES-GCM er randomiseret, så samme
        // pasnummer giver forskellig ciffertekst hver gang — selv et lighedsopslag ville
        // ikke virke. Ingen af de 126 porterede regler søger eller sorterer på feltet.
        builder.Ignore(guest => guest.FullName);

        builder.UseXminAsConcurrencyToken();
    }
}