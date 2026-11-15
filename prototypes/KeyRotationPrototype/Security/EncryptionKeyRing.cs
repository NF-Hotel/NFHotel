namespace KeyRotationPrototype.Security;


/// <summary>
/// Manages multiple AES encryption keys and tracks which key is currently active.
/// </summary>
/// <remarks>
/// This implementation is intended for prototype use only.
/// Keys are stored in memory and are not persisted.
/// </remarks>
public sealed class EncryptionKeyRing
{
    // Stores encryption keys by their unique version identifiers.
    //create a Dictionary that stores encryption keys.
    private readonly Dictionary<string, byte[]> _keys = new();

    /// <summary>
    /// Gets the identifier of the key used for new encryptions.
    /// </summary>
    public string ActiveKeyId { get; private set; }


    /// <summary>
    /// Initializes the key ring with an active key identifier.
    /// Constructor â€“ creation of Key Ring
    /// </summary>
    /// <param name="activeKeyId">
    /// Identifier of the key intended for encryption.
    /// The key must be added before it can be used.
    /// </param>
    public EncryptionKeyRing(string activeKeyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activeKeyId);
        ActiveKeyId = activeKeyId;

    }

    /// <summary>
    /// Adds an AES encryption key to the key ring.
    /// </summary>
    /// <param name="keyId">Unique key identifier, e.g. v1.</param>
    /// <param name="key">AES key bytes.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if the key length is invalid or the ID already exists.
    /// </exception>
    public void AddKey(string keyId, byte[] key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentNullException.ThrowIfNull(key);

        // AES supports 128, 192 and 256-bit keys (16, 24 and 32 bytes).
        if (key.Length is not (16 or 24 or 32))
        {
            throw new ArgumentException("AES Key must be 16, 24, or 32 bytes long.", nameof(key));
        }

        // Clone the key to prevent changes to the original array.
        if (!_keys.TryAdd(keyId, (byte[])key.Clone()))
        {
            throw new ArgumentException(
                $"Key ID '{keyId}' already exists.",
                nameof(keyId));
        }
    }


    /// <summary>
    /// Retrieves an encryption key by its identifier.
    /// </summary>
    /// <param name="keyId">Identifier of the requested key.</param>
    /// <returns>A copy of the AES key bytes.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the key does not exist.
    /// </exception>
    public byte[] GetKey(string keyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new KeyNotFoundException(
                $"Encryption key '{keyId}' was not found.");
        }

        // Return a copy to protect the stored key.
        return (byte[])key.Clone();
    }

    /// <summary>
    /// Changes the active encryption key.
    /// </summary>
    /// <param name="keyId">
    /// Identifier of an existing key.
    /// </param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the requested key does not exist.
    /// </exception>
    public void SetActiveKey(string keyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        // Only registered keys can become active.
        if (!_keys.ContainsKey(keyId))
        {
            throw new KeyNotFoundException(
                $"Cannot activate unknown key '{keyId}'.");
        }

        ActiveKeyId = keyId;
    }

}
