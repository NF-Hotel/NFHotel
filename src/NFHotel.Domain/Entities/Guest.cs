using NFHotel.Domain.Errors;
using NFHotel.Domain.Rules;

namespace NFHotel.Domain.Entities;

/// <summary>
/// En gæst. Aggregatrod.
/// </summary>
/// <remarks>
/// Ren domæneentitet — bevidst IKKE en login- eller kontoentitet.
/// Invarianterne håndhæves i <see cref="Create"/> og <see cref="UpdateDetails"/>, så en
/// persisteret, ugyldig gæst ikke kan opstå (BR-84 bliver strukturelt umulig).
/// </remarks>
public sealed class Guest : Entity<int>
{
    /// <summary>Længste tilladte for- og efternavn (A-10). Spejles af EF-konfigurationen.</summary>
    public const int MaxNameLength = 100;

    /// <summary>Længste tilladte e-mail (A-10).</summary>
    public const int MaxEmailLength = 100;

    /// <summary>Længste tilladte telefonnummer (A-10).</summary>
    public const int MaxPhoneNumberLength = 50;

    /// <summary>Længste tilladte landenavn (A-10).</summary>
    public const int MaxCountryLength = 100;

    /// <summary>Længste tilladte pasnummer (A-10).</summary>
    public const int MaxPassportNumberLength = 50;

    /// <summary>Til EF Core. Brug <see cref="Create"/> i kode.</summary>
    private Guest()
    {
    }

    /// <summary>Fornavn. Aldrig tomt eller kun whitespace (BR-91).</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Efternavn. Aldrig tomt eller kun whitespace (BR-92).</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>E-mail. Indeholder altid både '@' og '.' (BR-93).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Telefonnummer. Aldrig tomt; intet formatkrav (BR-94).</summary>
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>Land. Aldrig tomt (BR-95).</summary>
    public string Country { get; private set; } = string.Empty;

    /// <summary>
    /// Pasnummer. Valgfrit og uvalideret ud over længden (BR-57).
    /// </summary>
    /// <remarks>
    /// Følsomt: må ikke bruges som søge- eller sorteringsnøgle (B-09, D-04). Domænet kender
    /// ikke krypteringen — den er infrastrukturens ansvar.
    /// </remarks>
    public string? PassportNumber { get; private set; }

    /// <summary>
    /// Fuldt navn, "Fornavn Efternavn". Bruges af Application-lagets søgning og sortering
    /// (BR-25, BR-68, BR-69). Domæneidentitet, ikke en visningsstreng: ingen kultur,
    /// intet format, ingen afkortning.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Opretter en ny gæst.
    /// </summary>
    /// <param name="firstName">Fornavn.</param>
    /// <param name="lastName">Efternavn.</param>
    /// <param name="email">E-mail. Skal indeholde både '@' og '.'.</param>
    /// <param name="phoneNumber">Telefonnummer.</param>
    /// <param name="country">Land.</param>
    /// <param name="passportNumber">Pasnummer. Valgfrit.</param>
    /// <returns>En ny, gyldig gæst der endnu ikke er persisteret.</returns>
    /// <exception cref="DomainException">
    /// Ved brud på BR-91 til BR-95 eller en længdegrænse (A-10). Fejlkoden er den første
    /// kode fra <see cref="GuestRules.Validate"/>.
    /// </exception>
    public static Guest Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string country,
        string? passportNumber)
    {
        var guest = new Guest();
        guest.Apply(firstName, lastName, email, phoneNumber, country, passportNumber);

        return guest;
    }

    /// <summary>
    /// Overskriver præcis de seks redigérbare felter. <see cref="Entity{TKey}.Id"/> ændres aldrig (BR-72).
    /// </summary>
    /// <param name="firstName">Fornavn.</param>
    /// <param name="lastName">Efternavn.</param>
    /// <param name="email">E-mail.</param>
    /// <param name="phoneNumber">Telefonnummer.</param>
    /// <param name="country">Land.</param>
    /// <param name="passportNumber">Pasnummer. Valgfrit.</param>
    /// <exception cref="DomainException">Samme invarianter som <see cref="Create"/>.</exception>
    public void UpdateDetails(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string country,
        string? passportNumber) =>
        Apply(firstName, lastName, email, phoneNumber, country, passportNumber);

    /// <summary>
    /// Håndhæver invarianterne og skriver værdierne. Delt af <see cref="Create"/> og
    /// <see cref="UpdateDetails"/>, så reglen kun findes ét sted.
    /// </summary>
    private void Apply(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string country,
        string? passportNumber)
    {
        var errors = GuestRules.Validate(firstName, lastName, email, phoneNumber, country, passportNumber);

        if (errors.Count > 0)
        {
            throw new DomainException(errors[0]);
        }

        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        Country = country;
        PassportNumber = passportNumber;
    }
}