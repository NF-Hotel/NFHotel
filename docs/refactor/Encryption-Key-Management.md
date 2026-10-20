# Encryption Key Management — NFHotel
> IT security · D-04 (Privacy by Design) and D-07 (Secrets Management), cf. `Decisions.md`.
>
> **Status: Draft — awaiting review.** The final solution for storing encryption keys in production has not been decided yet

---

## 1. Purpose
This document sets out the principles for secure handling of encryption keys in NFHotel, based on D-04
(Privacy by Design) and D-07 (Secrets Management)

NFHotel uses **AES-GCM** to encrypt guests' passport numbers when they are stored in PostgreSQL.

The purpose of the key management is to ensure that:

- Encryption keys are generated with a cryptographically secure random number generator.
- Keys are stored separately from the source code and never committed to Git.
- Access to these keys is limited to authorised persons/roles and components only.
- A procedure is defined for key rotation, compromise and loss of keys

**NOTE** The document distinguishes between the **existing implementation**, **recommended security measures** and **outstanding decisions**.


---


## 2. Existing implementation

NFHotel has implemented encryption of guests' passport numbers using
AES-GCM (Advanced Encryption Standard – Galois/Counter Mode).

The encryption is handled in the Infrastructure layer and is integrated with
Entity Framework Core (EF Core), so passport numbers are encrypted on storage and decrypted on reading from PostgreSQL.


### 2.1 AES-GCM encryption

The class `AesGcmStringEncryptor` handles encryption and decryption
of passport numbers through the interface `IStringEncryptor`.

The implementation uses:

- AES-GCM for encryption and integrity protection.
- A random nonce of 12 bytes on every encryption.
- An authentication tag of 16 bytes.
- An AES key of 128, 192 or 256 bits.

A new nonce is generated on every encryption, so the same passport number
normally results in different ciphertext.

The authentication tag is used to detect changes to the encrypted data.
If the check fails, the decryption is rejected.

### 2.2 EF Core integration

NFHotel uses `EncryptedStringConverter` for automatic encryption
and decryption of passport numbers through EF Core.

The converter is configured in `GuestConfiguration` on the field
`PassportNumber`.

- On storage the passport number is encrypted before it is saved in PostgreSQL.
- On reading the passport number is decrypted through EF Core.
- If `PassportNumber` is `null`, the database value remains `NULL`.

The passport number has no database index, since the AES-GCM
encryption used does not support ordinary search or
sorting on the encrypted value.

### 2.3 Configuration and validation

The encryption key is read from the configuration through
`EncryptionOptions` with the configuration name `Encryption:PassportKey`.

`AesGcmStringEncryptor` validates that the key:

- Is specified.
- Is valid Base64.
- Has a valid AES key length of 128, 192 or 256 bits.

If the key is missing or invalid, the configuration is rejected
at application start-up.

The existing validation does not in itself ensure that the key
is stored securely.

### 2.4 Tests

The implementation is verified through unit tests and
PostgreSQL integration tests.

The tests check, among other things:

- That encrypted values can be decrypted correctly.
- That repeated encryption gives different results.
- That tampering with the ciphertext is detected.
- That invalid and missing keys are rejected.
- That passport numbers are stored encrypted in PostgreSQL.
- That EF Core decrypts values correctly on reading.
- That a missing passport number remains `NULL` in the database.

The tests use test keys and not production keys.



### 2.5 Scope of encryption

AES-GCM encryption is currently applied to guests' passport numbers.

First name, last name, email and country remain searchable to support guest search requirements BR-68 / BR-N-05.

These fields must still be protected through appropriate access control and database security measures.

Encryption of additional sensitive personal data requires a separate assessment and team approval.


---


## 3. Key inventory

A key inventory is used to document which cryptographic
keys the system uses, their purpose and how they are handled.

This supports the principle that a cryptographic key should only
be used for one specific purpose (Key Separation), as recommended
by OWASP and NIST.


### 3.1 Existing encryption key

NFHotel uses an AES key to encrypt guests' passport numbers.

| Property | Description |
|---|---|
| Key | `Encryption:PassportKey` |
| Purpose | Encryption and decryption of guests' passport numbers |
| Algorithm | AES-GCM |
| Key length | 128, 192 or 256 bits |
| Used by | `AesGcmStringEncryptor` in the Infrastructure layer |
| Configuration | `EncryptionOptions` |
| Storage | .NET User Secrets locally; production solution not decided |
| Rotation | Not implemented |

The encryption key itself must never be written in the documentation or committed to Git.

### 3.2 Separation of cryptographic keys

Encryption keys should only be used for their intended purpose.

The AES key for passport numbers must therefore not be reused for, for example:

- Signing JWT tokens.
- Encrypting other data types without a separate assessment.
- Other cryptographic functions with different security requirements.

This limits the consequences if a key is compromised.


---


## 4. Generation of encryption keys

Encryption keys must be generated with a cryptographically secure
random number generator (CSPRNG).

The following principles apply to NFHotel:

- AES-256 bit is recommended for new encryption keys.
- Separate keys must be used for the development and production environments


The shared development key for `Encryption:PassportKey` was
generated by the DevOps lead.

---


## 5. Storage and access control

Encryption keys must be stored separately from the source code and only be available to authorised persons.

- **Local development:** `Encryption:PassportKey` is stored in .NET User Secrets.
- **Shared development database:** The applications use the same AES key so that they can decrypt existing passport numbers.
- **Access control:** Only developers with a specific need should have access to the encryption key.
- **Production:** The key should be stored in a secure secret-management system. The specific solution has not been decided yet.

User Secrets are not encrypted and must therefore not be regarded as secure key storage for production.

---


## 6. Key rotation and compromise

Key rotation of course means that an existing encryption key is replaced by a new one.

NFHotel does not yet have an implemented solution for key rotation.

When `Encryption:PassportKey` is replaced, the following must be ensured:

- Existing passport numbers must be decryptable with the old key and encrypted again with the new one
- The old key may only be phased out when the migration has been completed
  and verified.
- If a compromise is suspected, access to the key must be restricted,
  and affected data and systems investigated.

**Important:** If the key is simply replaced in the configuration, existing encrypted passport numbers can no longer be decrypted.

The cybersecurity lead prepares a procedure for key rotation and handling of compromised keys in cooperation with DevOps.


---

## 7. Backup and recovery
NFHotel must ensure that encryption keys can be recovered after a system failure or loss, so that encrypted passport numbers do not become
permanently unavailable.

- Backups of encryption keys must be stored securely and separately from database backups.
- Access to backup keys must be limited to authorised persons.
- Recovery must be tested so that it is verified that encrypted data can still be decrypted.

**Status:** A specific
backup and recovery procedure for `Encryption:PassportKey` has not been documented yet.

---

## 8. Responsibility and outstanding decisions

The cybersecurity lead prepares security requirements and procedures
for key management in cooperation with DevOps and the rest of the team.

The following decisions are still missing:

| Area | Outstanding decision |

|---|---|
| Production | Choice of secret-management solution |
| Key rotation | Specific procedure and technical implementation |
| Backup | Secure storage and testing of recovery |


The decisions must be clarified and approved by the team before the solution is used in production.


### 8.1 D-04 assessment

The existing AES-GCM implementation protects guests' passport numbers and has been verified through unit and integration tests.

The encryption scope has been reviewed against the guest data model and search requirements BR-68 / BR-N-05.

First name, last name, email and country remain searchable. Phone numbers are also personal data but are not currently included in the application-level AES-GCM encryption scope.

No additional field-level encryption is proposed as part of the current D-04 task.

Production key storage, key rotation and backup/recovery remain outstanding decisions and must be addressed before production deployment.

---

## 9. References

- [OWASP – Key Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Key_Management_Cheat_Sheet.html)
- [Microsoft – Safe Storage of App Secrets in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)
- [NIST SP 800-57 – Recommendation for Key Management](https://csrc.nist.gov/pubs/sp/800/57/pt1/r5/final)
- Internal decisions: `Decisions.md` (D-04 and D-07).
