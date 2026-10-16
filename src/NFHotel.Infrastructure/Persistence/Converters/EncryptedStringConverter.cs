using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence.Converters;

/// <summary>
/// Krypterer en streng på vej til databasen og dekrypterer den på vej tilbage (B-09).
/// </summary>
/// <remarks>
/// EF Core kalder <b>ikke</b> value converters for <c>null</c>, så <c>NULL</c> forbliver
/// <c>NULL</c> i databasen. Det er forudsætningen for at et pasnummer kan fjernes igen og
/// ende som <c>NULL</c> frem for som en krypteret tom streng (T-41).
/// <para>
/// Konsekvenserne af den randomiserede ciffer er absolutte: kolonnen kan hverken indekseres,
/// sorteres eller søges på. Se
/// <see cref="Configurations.GuestConfiguration"/>.
/// </para>
/// </remarks>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    /// <summary>
    /// Opretter konverteren.
    /// </summary>
    /// <param name="encryptor">Krypteringstjenesten der bærer nøglen.</param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="encryptor"/> er <c>null</c>.</exception>
    public EncryptedStringConverter(IStringEncryptor encryptor)
        : base(
            plainText => encryptor.Encrypt(plainText),
            cipherText => encryptor.Decrypt(cipherText))
    {
        ArgumentNullException.ThrowIfNull(encryptor);
    }
}