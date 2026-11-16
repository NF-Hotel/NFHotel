# Encryption Key Rotation Prototype — NFHotel

> IT security · D-04 (Privacy by Design) and D-07 (Secrets Management), cf. `Decisions.md`.
>
> **Status: Prototype implemented and tested — awaiting review.** Key rotation has not been integrated into the existing NFHotel application.

---

## 1. Purpose

This document describes the implementation and testing of an encryption key rotation prototype for NFHotel, based on D-04 (Privacy by Design) and D-07 (Secrets Management).

NFHotel currently uses **AES-GCM** to encrypt guests' passport numbers before they are stored in PostgreSQL.

The existing encryption implementation uses a configured encryption key but does not support encryption key rotation.

If the encryption key is replaced without migrating existing encrypted data, previously encrypted passport numbers can no longer be decrypted.

The purpose of the prototype is to investigate and demonstrate how encryption keys can be rotated while maintaining access to previously encrypted data.

The prototype focuses on:

- Changing the active encryption key from one version to another.
- Maintaining access to data encrypted with previous key versions.
- Re-encrypting existing data using a new encryption key.
- Investigating how key rotation can support the handling of compromised encryption keys.

**NOTE:** This document distinguishes between the **implemented prototype**, **recommended security measures** and **outstanding decisions**. The prototype is not a production-ready key management solution.

---

## 2. Existing implementation

NFHotel uses `AesGcmStringEncryptor` in the Infrastructure layer to encrypt and decrypt guests' passport numbers.

The existing implementation uses:

- AES-GCM encryption.
- A randomly generated nonce of 12 bytes.
- An authentication tag of 16 bytes.
- An AES key of 128, 192 or 256 bits.
- Base64 encoding of the encrypted output.

The encrypted data follows the binary format:

`[Nonce][Authentication Tag][Ciphertext]`

The encryption key is configured through `Encryption:PassportKey`.

### 2.1 Limitations of the existing implementation

The existing implementation does not include a key version identifier in the encrypted output.

This means that the application cannot determine which encryption key was originally used for a stored value.

Replacing `Encryption:PassportKey` without first migrating existing encrypted data would prevent the application from decrypting that data.

The prototype investigates how key versioning and re-encryption could address this limitation.

---

## 3. Prototype implementation

The prototype is implemented as a separate .NET console application under `prototypes/KeyRotationPrototype/`.

Unit tests are located in `prototypes/KeyRotationPrototype.Tests/`.

The implementation contains two main classes:

| Class | Responsibility |
|---|---|
| `EncryptionKeyRing` | Stores available encryption keys and identifies the currently active key |
| `VersionedAesGcmEncryptor` | Encrypts, decrypts and re-encrypts data using versioned AES-GCM keys |

### 3.1 EncryptionKeyRing

`EncryptionKeyRing` manages multiple encryption keys identified by a key version, such as `v1` or `v2`.

The class supports:

- Registering encryption keys using `AddKey()`.
- Retrieving an existing key using `GetKey()`.
- Changing the active encryption key using `SetActiveKey()`.
- Identifying the active key through `ActiveKeyId`.

The class validates AES key lengths and rejects duplicate key identifiers.

Encryption keys are stored in memory only. The prototype does not implement persistent or production-grade key storage.

### 3.2 VersionedAesGcmEncryptor

`VersionedAesGcmEncryptor` performs encryption and decryption using the keys managed by `EncryptionKeyRing`.

The implementation uses AES-GCM with a randomly generated 12-byte nonce and a 16-byte authentication tag.

The encrypted output includes a key version identifier:

`v1:[Base64 encrypted data]`

The Base64 data contains:

`[Nonce][Authentication Tag][Ciphertext]`

The key version allows the decryptor to identify which registered encryption key must be used.

The class provides:

- `Encrypt()` — encrypts plaintext using the active key.
- `Decrypt()` — decrypts data using the corresponding key version.
- `ReEncrypt()` — decrypts an existing value and encrypts it again using the active key.

### 3.3 Backward compatibility

The prototype supports encrypted values without a key version prefix.

These values are treated as legacy data encrypted with key version `v1`.

This allows the prototype to demonstrate compatibility with the existing NFHotel ciphertext format.

**Important:** In a real migration, the original encryption key must remain available. Registering a newly generated key as `v1` would not allow existing passport numbers to be decrypted.

---

## 4. Key rotation procedure

The prototype demonstrates a rotation from encryption key `v1` to `v2`.

The procedure consists of the following steps:

1. Generate and register two AES-256 encryption keys.
2. Encrypt test data using the active `v1` key.
3. Change the active encryption key to `v2`.
4. Verify that the original encrypted data can still be decrypted using `v1`.
5. Re-encrypt the existing data using `v2`.
6. Verify that the re-encrypted data can be decrypted correctly.

The rotation changes which key is used for new encryption operations.

Previously encrypted values remain associated with their original key version until they are re-encrypted.

### 4.1 Re-encryption of existing data

The `ReEncrypt()` method performs the following operations:

- Decrypts the existing ciphertext using its original encryption key.
- Encrypts the recovered plaintext using the currently active key.
- Returns a new encrypted value containing the active key version.

AES-GCM generates a new random nonce during re-encryption.

The prototype performs these operations in memory.

It does not update PostgreSQL records or automatically migrate existing guest data.

### 4.2 Retirement of old encryption keys

Old encryption keys must remain available while data encrypted with those keys still exists.

An old key should only be retired after the migration has been completed and verified, including any relevant backup and recovery requirements.

**Status:** The prototype demonstrates key rotation and re-encryption but does not implement automatic key retirement.

---

## 5. Testing and validation

The prototype is verified through six automated xUnit tests.

The tests cover:

- Encryption and decryption using the same key.
- Decryption of existing data after changing the active key.
- Decryption of legacy ciphertext without a version prefix.
- Detection of modified encrypted data.
- Rejection of decryption using an incorrect key.
- Re-encryption from `v1` to `v2` while preserving the original plaintext.

All six unit tests passed during local testing.

The tests use randomly generated test keys and fictional passport numbers.

### 5.1 Console demonstration

The console application demonstrates the key rotation procedure using fictional passport data.

The demonstration verifies that:

- Data can be encrypted using `v1`.
- The active encryption key can be changed to `v2`.
- Data encrypted with `v1` remains decryptable.
- Existing ciphertext can be re-encrypted using `v2`.
- The re-encrypted data can be decrypted successfully.

The console demonstration completed successfully during local testing.

### 5.2 Running the prototype

The console application can be executed from the repository root using:

`dotnet run --project prototypes/KeyRotationPrototype/KeyRotationPrototype.csproj`

The unit tests can be executed using:

`dotnet test prototypes/KeyRotationPrototype.Tests/KeyRotationPrototype.Tests.csproj`

---

## 6. Security considerations

The prototype demonstrates the technical principles of encryption key rotation but does not provide a complete production key management solution.

The following security considerations must be addressed before integration into NFHotel.

### 6.1 Secure key storage

The prototype stores encryption keys in memory and generates temporary keys for demonstration purposes.

A production implementation requires a secure method for storing, accessing and recovering encryption keys.

Encryption keys must never be committed to Git or written directly into source code.

### 6.2 Authentication of key version information

The prototype stores the key version as a prefix outside the AES-GCM encrypted payload.

The version identifier is not currently authenticated using AES-GCM Additional Authenticated Data (AAD).

A production implementation should cryptographically bind the key version to the encrypted data and define a validated ciphertext format.

### 6.3 Key compromise

Key rotation can support the response to a compromised encryption key.

However, rotating a key does not reverse any previous exposure of encrypted data or plaintext.

A suspected compromise requires investigation, restriction of access, assessment of affected data and a controlled migration procedure.

### 6.4 Migration and recovery

The prototype does not implement:

- Database migration of encrypted passport numbers.
- Persistent storage of encryption key versions.
- Automatic retirement of old encryption keys.
- Secure key backup and recovery.
- Transaction handling or recovery after interrupted migrations.

These areas require further design and testing before production use.

---

## 7. Future integration with NFHotel

The prototype is currently isolated from the existing NFHotel application.

Integration would require changes to the existing encryption implementation and key management configuration.

Before integration, the team must determine:

- How encryption keys and their versions will be stored securely.
- How the existing `AesGcmStringEncryptor` and `IStringEncryptor` implementation should support versioned encryption.
- How existing encrypted passport numbers will be migrated.
- How migration progress and correctness will be verified.
- How old keys and encrypted backups will be handled.
- How interrupted migrations and failed decryption operations will be recovered.

The existing encryption implementation must remain compatible with stored passport data throughout any approved migration.

**Status:** No changes to NFHotel's existing encryption services or PostgreSQL data have been made as part of this prototype.

---

## 8. Responsibility and outstanding decisions

The cybersecurity lead investigates encryption key rotation and documents the prototype in cooperation with DevOps and the rest of the team.

The following decisions remain outstanding:

| Area | Outstanding decision |
|---|---|
| Production key storage | Selection of a secure secret-management solution |
| Key versioning | Final authenticated ciphertext format |
| Integration | Changes required to the existing encryption services |
| Database migration | Procedure for re-encrypting existing passport numbers |
| Key retirement | Criteria for safely removing old encryption keys |
| Backup and recovery | Recovery of keys and encrypted data after rotation |
| Deployment | Approval and validation of the production migration procedure |

These decisions must be reviewed and approved by the team before the prototype can be considered for production integration.

---

## 9. References

- [OWASP – Key Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Key_Management_Cheat_Sheet.html)
- [Microsoft – Safe Storage of App Secrets in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)
- [NIST SP 800-57 – Recommendation for Key Management](https://csrc.nist.gov/pubs/sp/800/57/pt1/r5/final)
- Internal documentation: `Encryption-Key-Management.md`.
- Internal decisions: `Decisions.md` (D-04 and D-07).