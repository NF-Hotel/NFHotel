using System.Reflection;

using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Booking;

namespace NFHotel.Web.Tests;

/// <summary>
/// The errors of the guest form stand at their fields (BR-60, BR-96), the codes of the rules that MIL-086 added
/// included: every code of <see cref="ErrorCodes.Guest"/> that names a field is shown at that field and not above the
/// form. The codes are read from <see cref="ErrorCodes.Guest"/> itself, so a code that is added later is tested too.
/// </summary>
public sealed class GuestFieldErrorsTests
{
    private static readonly string[] Fields = ["FirstName", "LastName", "Email", "PhoneNumber", "Country", "PassportNumber"];

    /// <summary>Every code of a guest field, with the field that its name begins with.</summary>
    /// <returns>The pairs of code and field.</returns>
    public static TheoryData<string, string> CodesOfTheFields()
    {
        TheoryData<string, string> data = new();
        foreach ((string code, string field) in FieldOfEachCode())
        {
            data.Add(code, field);
        }

        return data;
    }

    /// <summary>Equivalence: the code of a field is shown at that field, and not above the form.</summary>
    /// <param name="code">The code.</param>
    /// <param name="field">The field that the code names.</param>
    [Theory]
    [MemberData(nameof(CodesOfTheFields))]
    public void FromResult_CodeOfAField_StandsAtThatField(string code, string field)
    {
        // Arrange
        Error error = new(code);

        // Act
        GuestFieldErrors errors = GuestFieldErrors.FromResult(Result.Failure([error]));

        // Assert
        Assert.Equal(ErrorMessages.Describe(error), TextAt(errors, field));
        Assert.Empty(errors.Other);
    }

    /// <summary>The codes are read for every field, the codes of the rules of MIL-086 among them.</summary>
    [Fact]
    public void FieldOfEachCode_TheCodesOfTheRules_AreRead()
    {
        // Act
        List<(string Code, string Field)> codes = FieldOfEachCode();

        // Assert
        Assert.All(Fields, field => Assert.Contains(codes, entry => entry.Field == field));
        Assert.Contains((ErrorCodes.Guest.FirstNameInvalid, "FirstName"), codes);
        Assert.Contains((ErrorCodes.Guest.LastNameInvalid, "LastName"), codes);
        Assert.Contains((ErrorCodes.Guest.PhoneNumberInvalid, "PhoneNumber"), codes);
        Assert.Contains((ErrorCodes.Guest.CountryInvalid, "Country"), codes);
        Assert.Contains((ErrorCodes.Guest.PassportNumberInvalid, "PassportNumber"), codes);
    }

    private static List<(string Code, string Field)> FieldOfEachCode()
    {
        List<(string Code, string Field)> codes = [];
        foreach (FieldInfo constant in typeof(ErrorCodes.Guest).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (constant.IsLiteral
                && constant.GetRawConstantValue() is string code
                && Fields.FirstOrDefault(field => constant.Name.StartsWith(field, StringComparison.Ordinal)) is { } field)
            {
                codes.Add((code, field));
            }
        }

        return codes;
    }

    private static string? TextAt(GuestFieldErrors errors, string field) => field switch
    {
        "FirstName" => errors.FirstName,
        "LastName" => errors.LastName,
        "Email" => errors.Email,
        "PhoneNumber" => errors.PhoneNumber,
        "Country" => errors.Country,
        "PassportNumber" => errors.PassportNumber,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Not a field of the guest."),
    };
}
