namespace NFHotel.Infrastructure.Security;

/// <summary>
/// Symmetrisk kryptering af enkeltfelter at-rest (B-09, D-04).
/// </summary>
/// <remarks>
/// Interfacet bor i Infrastructure og ikke i Application: kryptering er en
/// persisteringsdetalje. Domænet kender kun klartekst — <c>Guest.PassportNumber</c> ved
/// ikke at feltet krypteres, og skal ikke vide det.
/// <para>
/// Implementeringen er randomiseret (ny nonce pr. kryptering), så samme klartekst giver
/// forskellig ciffertekst hver gang. Konsekvensen er absolut: feltet kan hverken indekseres,
/// sorteres eller søges på — heller ikke med lighed.
/// </para>
/// </remarks>
public interface IStringEncryptor
{
    /// <summary>
    /// Krypterer en klartekstværdi.
    /// </summary>
    /// <param name="plainText">Klarteksten.</param>
    /// <returns>Ciffertekst i et selvbeskrivende, base64-kodet format.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Dekrypterer en ciffertekst produceret af <see cref="Encrypt"/>.
    /// </summary>
    /// <param name="cipherText">Cifferteksten.</param>
    /// <returns>Den oprindelige klartekst.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Kastes hvis værdien er beskadiget, forkortet eller krypteret med en anden nøgle.
    /// </exception>
    string Decrypt(string cipherText);
}