namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// Passwords that are refused because anyone guessing would try them first (ADR-0017). Only passwords of the
/// minimum length or more are listed, as shorter ones fail the length rule. A password made of one repeated
/// character is refused too. The list is compared without regard to case.
/// </summary>
internal static class CommonPasswords
{
    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "password1234",
        "password12345",
        "password123456",
        "passw0rd1234",
        "password!234",
        "changeme1234",
        "changemenow1",
        "welcome12345",
        "welcome12345!",
        "welcome123456",
        "letmein12345",
        "letmein123456",
        "administrator",
        "administrator1",
        "admin1234567",
        "admin12345678",
        "adminadmin12",
        "qwertyuiop12",
        "qwertyuiopas",
        "qwertyuiopasdf",
        "qwerty123456",
        "qwerty1234567",
        "asdfghjkl123",
        "asdfghjklqwe",
        "zxcvbnm12345",
        "1q2w3e4r5t6y",
        "1qaz2wsx3edc",
        "1qaz2wsx3edc4rfv",
        "123456789012",
        "1234567890123",
        "12345678901234",
        "123456123456",
        "111111111111",
        "000000000000",
        "abcdefghijkl",
        "abcd12345678",
        "abc123456789",
        "iloveyou1234",
        "iloveyou12345",
        "monkey123456",
        "dragon123456",
        "football1234",
        "baseball1234",
        "superman1234",
        "trustno1trustno1",
        "sunshine1234",
        "princess1234",
        "master123456",
        "shadow123456",
        "michael12345",
        "charlie12345",
        "jennifer1234",
        "starwars1234",
        "whatever1234",
        "freedom12345",
        "hunter123456",
        "summer202512",
        "winter202512",
        "spring202512",
        "autumn202512",
        "password2024",
        "password2025",
        "password2026",
        "password2027",
        "nfhotelnfhotel",
        "nfhotel12345",
        "nfhotel123456",
        "nfhotel2026!",
        "hotelhotel12",
        "hotel1234567",
        "hotel12345678",
        "receptionist1",
        "receptionist12",
        "housekeeping1",
        "housekeeping12",
        "manager123456",
        "manager1234567",
    };

    /// <summary>Tells whether a password is on the list or is one repeated character.</summary>
    /// <param name="password">The password as typed.</param>
    /// <returns>True when it must be refused.</returns>
    public static bool IsCommon(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        return password.Length > 0 && (password.All(character => character == password[0]) || Known.Contains(password));
    }
}
