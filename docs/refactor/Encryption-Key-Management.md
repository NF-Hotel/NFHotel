# Encryption Key Management — NFHotel
> IT-sikkerhed · D-04 (Privacy by Design) og D-07 (Secrets Management), jf. `Decisions.md`.
>
> **Status: Udkast — afventer review.** Den endelige løsning til opbevaring af krypteringsnøgler i produktion er ikke besluttet endnu

---

## 1. Formål 
Dette dokument fastlægger principperne for sikker håndtering af krypteringsnøgler i NFHotel med udgangspunkt i D-04
(Privacy by Design) og D-07 (Secrets Management)

NFHotel anvender **AES-GCM** til kryptering af gæsters pasnumre ved lagring i PostgreSQL.

Formålet med nøglehåndteringen er at sikre, at:

- Krypteringsnøgler bliver genereret med en kryptografisk sikker tilfældighedsgenerator.
- Nøgler opbevares adskilt fra kildekoden og aldrig committes til Git.
- Adgang til disse nøgler begrænses kun til autoriserede personer/roller og komponenter.
- Der fastlægges procedure for nøglerotation, kompromittering og tab af nøgler

**BEMÆRK** Dokumentet skelner mellem **eksisterende implementering**, **anbefalede sikkerhedsforanstaltninger** og **udestående beslutninger**.


---


## 2. Eksisterende implementering 

NFHotel har implementeret kryptering af gæsters pasnumre ved hjælp af
AES-GCM (Advanced Encryption Standard – Galois/Counter Mode).

Krypteringen håndteres i Infrastructure-laget og er integreret med
Entity Framework Core (EF Core), så pasnumre krypteres ved lagring og dekrypteres ved læsning fra PostgreSQL.


### 2.1 AES-GCM-kryptering

Klassen `AesGcmStringEncryptor` håndterer kryptering og dekryptering
af pasnumre gennem interfacet `IStringEncryptor`.

Implementeringen anvender:

- AES-GCM til kryptering og integritetsbeskyttelse.
- En tilfældig nonce på 12 bytes ved hver kryptering.
- Et authentication tag på 16 bytes.
- En AES-nøgle på 128, 192 eller 256 bit.

En ny nonce genereres ved hver kryptering, så det samme pasnummer
normalt resulterer i forskellig ciphertext.

Authentication tag bruges til at opdage ændringer i de krypterede data.
Hvis kontrollen fejler, afvises dekrypteringen.

### 2.2 EF Core-integration

NFHotel anvender `EncryptedStringConverter` til automatisk kryptering
og dekryptering af pasnumre gennem EF Core.

Converteren er konfigureret i `GuestConfiguration` på feltet
`PassportNumber`.

- Ved lagring krypteres pasnummeret, inden det gemmes i PostgreSQL.
- Ved læsning dekrypteres pasnummeret gennem EF Core.
- Hvis `PassportNumber` er `null`, forbliver databaseværdien `NULL`.

Pasnummeret har ikke et databaseindeks, da den anvendte
AES-GCM-kryptering ikke understøtter almindelig søgning eller
sortering på den krypterede værdi.

### 2.3 Konfiguration og validering

Krypteringsnøglen læses fra konfigurationen gennem
`EncryptionOptions` med konfigurationsnavnet `Encryption:PassportKey`.

`AesGcmStringEncryptor` validerer, at nøglen:

- Er angivet.
- Er gyldigt Base64-format.
- Har en gyldig AES-nøglelængde på 128, 192 eller 256 bit.

Hvis nøglen mangler eller er ugyldig, afvises konfigurationen
ved applikationens opstart.

Den eksisterende validering sikrer ikke i sig selv, at nøglen
opbevares sikkert.

### 2.4 Tests

Implementeringen er verificeret gennem unit tests og
PostgreSQL-integrationstests.

Testene kontrollerer blandt andet:

- At krypterede værdier kan dekrypteres korrekt.
- At gentagen kryptering giver forskellige resultater.
- At manipulation med ciphertext opdages.
- At ugyldige og manglende nøgler afvises.
- At pasnumre lagres krypteret i PostgreSQL.
- At EF Core dekrypterer værdier korrekt ved læsning.
- At et manglende pasnummer forbliver `NULL` i databasen.

Testene anvender testnøgler og ikke produktionsnøgler.

---


## 3. Nøgleoversigt (Key Inventory)

En nøgleoversigt bruges til at dokumentere hvilke kryptografiske
nøgler systemet anvender, deres formål og hvordan de håndteres.

Dette understøtter princippet om, at en kryptografisk nøgle kun
bør anvendes til ét bestemt formål (Key Separation), som anbefalet
af OWASP og NIST.


### 3.1 Eksisterende krypteringsnøgle

NFHotel anvender en AES-nøgle til kryptering af gæsternes pasnumre.

| Egenskab | Beskrivelse |
|---|---|
| Nøgle | `Encryption:PassportKey` |
| Formål | Kryptering og dekryptering af gæsters pasnumre |
| Algoritme | AES-GCM |
| Nøglelængde | 128, 192 eller 256 bit |
| Anvendelse | `AesGcmStringEncryptor` i Infrastructure-laget |
| Konfiguration | `EncryptionOptions` |
| Opbevaring | .NET User Secrets lokalt; produktionsløsning ikke besluttet |
| Rotation | Ikke implementeret |

Selve krypteringsnøglen må aldrig skrives i dokumentationen eller committes til Git.

### 3.2 Adskillelse af kryptografiske nøgler

Krypteringsnøgler bør kun anvendes til deres tilsigtede formål.

AES-nøglen til pasnumre må derfor ikke genbruges til eksempelvis:

- Signering af JWT-tokens.
- Kryptering af andre datatyper uden en særskilt vurdering.
- Andre kryptografiske funktioner med forskellige sikkerhedskrav.

Dette begrænser konsekvenserne, hvis en nøgle bliver kompromitteret.


---


## 4. Generering af krypteringsnøgler

Krypteringsnøgler skal genereres med en kryptografisk sikker
tilfældighedsgenerator (CSPRNG).

For NFHotel gælder følgende principper:

- AES-256 bit anbefales til nye krypteringsnøgler.
- Udviklings og produktionsmiljø skal anvendes seperate nøgler 


Den fælles udviklingsnøgle til `Encryption:PassportKey` er
genereret af den DevOps-ansvarlige.

---


## 5. Opbevaring og adgangskontrol

Krypteringsnøgler skal opbevares adskilt fra kildekoden og kun være tilgængelige for autoriserede personer.

- **Lokal udvikling:** `Encryption:PassportKey` opbevares i .NET User Secrets.
- **Fælles udviklingsdatabase:** Applikationerne anvender samme AES-nøgle for at kunne dekryptere eksisterende pasnumre.
- **Adgangskontrol:** Kun udviklere med et konkret behov bør have adgang til krypteringsnøglen.
- **Produktion:** Nøglen bør opbevares i et sikkert secret-management-system. Den konkrete løsning er endnu ikke besluttet.

User Secrets er ikke krypteret og må derfor ikke betragtes som sikker nøgleopbevaring til produktion.

---


## 6. Nøglerotation og kompromittering

Nøglerotation betyder selvfølgelig, at en eksisterende krypteringsnøgle udskiftes med en ny.

NFHotel har endnu ikke en implementeret løsning til nøglerotation.

Ved udskiftning af `Encryption:PassportKey` skal følgende sikres:

- Eksisterende pasnumre skal kunne dekrypteres med den gamele nøgle og kryptres igen med den nye 
- Den gamle nøgle må først udfases, når migreringen er gennemført
  og verificeret.
- Ved mistanke om kompromittering skal nøglens adgang begrænses,
  og berørte data og systemer undersøges.

**Vigtigt:** Hvis nøglen blot udskiftes i konfigurationen kan eksisterende krypterede pasnumre ikke længere dekrypteres.

Cybersecurity-ansvarlig udarbejder en procedure for nøglerotation og håndtering af kompromitterede nøgler i samarbejde med DevOps.


---

## 7. Backup og gendannelse
NFHotel skal sikre, at krypteringsnøgler kan gendannes ved systemfejl eller tab så krypterede pasnumre ikke bliver
permanent utilgængelige.

- Backup af krypteringsnøgler skal opbevares sikkert og adskilt fra databasebackups.
- Adgang til backupnøgler skal begrænses til autoriserede personer.
- Gendannelse skal testes så det verificeres, at krypterede data fortsat kan dekrypteres.

**Status:** Der er endnu ikke dokumenteret en konkret
backup- og gendannelsesprocedure for `Encryption:PassportKey`.

---

## 8. Ansvar og udestående beslutninger

Cybersecurity-ansvarlig udarbejder sikkerhedskrav og procedurer
for nøglehåndtering i samarbejde med DevOps og resten af teamet.

Følgende beslutninger mangler fortsat:

| Område | Udestående beslutning |

|---|---|
| Produktion | Valg af secret-management-løsning |
| Nøglerotation | Konkret procedure og teknisk implementering |
| Backup | Sikker opbevaring og test af gendannelse |


Beslutningerne skal afklares og godkendes af teamet inden løsningen anvendes i produktion.

---

## 9. Referencer

- [OWASP – Key Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Key_Management_Cheat_Sheet.html)
- [Microsoft – Safe Storage of App Secrets in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)
- [NIST SP 800-57 – Recommendation for Key Management](https://csrc.nist.gov/pubs/sp/800/57/pt1/r5/final)
- Interne beslutninger: `Decisions.md` (D-04 og D-07).