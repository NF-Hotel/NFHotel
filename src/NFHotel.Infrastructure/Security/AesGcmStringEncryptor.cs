using System.Security.Cryptography;
using System.Text;

using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// AES-GCM-kryptering af enkeltfelter (B-09).
/// </summary>
/// <remarks>
/// Formatet er <c>base64(nonce ‖ tag ‖ ciphertext)</c> med faste længder på de to første
/// dele, så en værdi kan dekrypteres uden sidekanal-metadata.
/// <para>
/// AEAD er valgt frem for AES-CBC, fordi tag'et gør ændret ciffertekst til en
/// <see cref="CryptographicException"/> i stedet for til vrøvl-klartekst. Nonce'en trækkes
/// tilfældigt pr. kryptering, hvilket er præcis grunden til at feltet aldrig må indekseres:
/// samme pasnummer giver forskellig ciffertekst hver gang.
/// </para>
/// <para>
/// Typen er trådsikker: nøglen er uforanderlig, og <see cref="AesGcm"/> instantieres pr.
/// operation. Den kan derfor registreres som singleton.
/// </para>
/// </remarks>
public sealed class AesGcmStringEncryptor : IStringEncryptor
{
    private const int NonceLength = 12;
    private const int TagLength = 16;

    private static readonly int[] ValidKeyLengths = [16, 24, 32];

    private readonly byte[] _key;

    /// <summary>
    /// Opretter krypteringstjenesten.
    /// </summary>
    /// <param name="options">Krypteringsindstillingerne. Nøglen skal være base64-kodet.</param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="options"/> er <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Kastes hvis nøglen mangler, ikke er gyldig base64 eller ikke afkoder til 16, 24
    /// eller 32 bytes. Fejlen kastes ved opstart, ikke ved første brug (fail fast).
    /// </exception>
    public AesGcmStringEncryptor(EncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _key = DecodeKey(options.PassportKey);
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var output = new byte[NonceLength + TagLength + plainBytes.Length];

        var nonce = output.AsSpan(0, NonceLength);
        var tag = output.AsSpan(NonceLength, TagLength);
        var cipherText = output.AsSpan(NonceLength + TagLength);

        RandomNumberGenerator.Fill(nonce);

        using var aesGcm = new AesGcm(_key, TagLength);
        aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);

        return Convert.ToBase64String(output);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        var input = DecodeCipherText(cipherText);

        var nonce = input.AsSpan(0, NonceLength);
        var tag = input.AsSpan(NonceLength, TagLength);
        var payload = input.AsSpan(NonceLength + TagLength);
        var plainBytes = new byte[payload.Length];

        using var aesGcm = new AesGcm(_key, TagLength);
        aesGcm.Decrypt(nonce, payload, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>Afkoder og validerer nøglen. Fejler ved opstart frem for ved første brug.</summary>
    private static byte[] DecodeKey(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException(
                $"Krypteringsnøglen '{EncryptionOptions.SectionName}:{EncryptionOptions.PassportKeyName}' mangler. "
                + "Appen nægter at starte uden, så pasnumre ikke kan ende i klartekst (B-09).");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                $"Krypteringsnøglen '{EncryptionOptions.SectionName}:{EncryptionOptions.PassportKeyName}' "
                + "er ikke gyldig base64.",
                exception);
        }

        if (!ValidKeyLengths.Contains(key.Length))
        {
            throw new InvalidOperationException(
                $"Krypteringsnøglen skal afkode til 16, 24 eller 32 bytes, men fyldte {key.Length}.");
        }

        return key;
    }

    /// <summary>Afkoder cifferteksten og afviser værdier der er for korte til at bære nonce og tag.</summary>
    private static byte[] DecodeCipherText(string cipherText)
    {
        byte[] input;

        try
        {
            input = Convert.FromBase64String(cipherText);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("Den krypterede værdi er ikke gyldig base64.", exception);
        }

        if (input.Length < NonceLength + TagLength)
        {
            throw new CryptographicException("Den krypterede værdi er for kort til at bære nonce og tag.");
        }

        return input;
    }
}