using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence.Converters;

/// <summary>
/// Encrypts a string on its way to the database and decrypts it on its way back (B-09).
/// </summary>
/// <remarks>
/// EF Core does <b>not</b> call value converters for <c>null</c>, so <c>NULL</c> stays
/// <c>NULL</c> in the database. That is the precondition for a passport number being removable again and
/// ending as <c>NULL</c> rather than as an encrypted empty string (T-41).
/// <para>
/// The consequences of the randomised cipher are absolute: the column can be neither indexed,
/// sorted nor searched. See
/// <see cref="Configurations.GuestConfiguration"/>.
/// </para>
/// </remarks>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    /// <summary>
    /// Opretter konverteren.
    /// </summary>
    /// <param name="encryptor">The encryption service that carries the key.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="encryptor"/> is <c>null</c>.</exception>
    public EncryptedStringConverter(IStringEncryptor encryptor)
        : base(
            plainText => encryptor.Encrypt(plainText),
            cipherText => encryptor.Decrypt(cipherText))
    {
        ArgumentNullException.ThrowIfNull(encryptor);
    }
}