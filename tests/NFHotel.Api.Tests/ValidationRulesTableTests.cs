using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Controllers;
using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The table "Validation rules of the booking flow" of the rule ledger holds against the code (MIL-086 decision 1,
/// criterion 2, ADR-0050): every field of a request type of the flow and every query value of the flow has a row, every
/// row names a field that exists with its type, every text has a length, every code of the table is a code of the API,
/// every method of the last column exists, and every request type of the flow refuses a field that it does not have.
/// The rows of the address of the booking page are held by the tests of the web application.
/// </summary>
public sealed partial class ValidationRulesTableTests
{
    // The actions of the booking flow: the public booking, the booking request and its verification by email, the guest
    // sign-in, the move by a guest, the booking form, the move and the overview of the back office, the guest form, and
    // the queries of the free rooms and of the guest search. A new action of the flow is added here, and the table then
    // needs its rows.
    private static readonly (Type Controller, string Action)[] FlowActions =
    [
        (typeof(BookingsController), nameof(BookingsController.CreatePublicAsync)),
        (typeof(BookingsController), nameof(BookingsController.AskPublicBookingAsync)),
        (typeof(BookingsController), nameof(BookingsController.VerifyPublicBookingAsync)),
        (typeof(GuestAuthController), nameof(GuestAuthController.SignInAsync)),
        (typeof(GuestBookingController), nameof(GuestBookingController.RescheduleAsync)),
        (typeof(BookingsController), nameof(BookingsController.CreateAsync)),
        (typeof(BookingsController), nameof(BookingsController.RescheduleAsync)),
        (typeof(BookingsController), nameof(BookingsController.GetOverviewAsync)),
        (typeof(BookingsController), nameof(BookingsController.GetAvailableRoomsAsync)),
        (typeof(GuestsController), nameof(GuestsController.CreateAsync)),
        (typeof(GuestsController), nameof(GuestsController.UpdateAsync)),
        (typeof(GuestsController), nameof(GuestsController.SearchAsync)),
    ];

    // The assemblies whose types the last column may name.
    private static readonly Assembly[] RuleAssemblies =
    [
        typeof(Guest).Assembly,
        typeof(ErrorCodes).Assembly,
        typeof(BookingsController).Assembly,
    ];

    #region The table against the request types

    /// <summary>Positive: every field of a request type of the flow and every query value of the flow has a row.</summary>
    [Fact]
    public void Table_EveryFieldOfTheFlow_HasARow()
    {
        // Arrange
        HashSet<string> rows = ValidationRulesTable.Read().Select(row => row.Field).ToHashSet(StringComparer.Ordinal);

        // Act
        string[] missing = FieldsOfTheFlow().Keys.Where(field => !rows.Contains(field)).Order(StringComparer.Ordinal).ToArray();

        // Assert
        Assert.True(missing.Length == 0, "The table has no row for: " + string.Join(", ", missing));
    }

    /// <summary>Negative: a row that names a field that the flow does not have fails, so the table holds no stale row.</summary>
    [Fact]
    public void Table_EveryRow_NamesAFieldOfTheFlow()
    {
        // Arrange
        Dictionary<string, Type> fields = FieldsOfTheFlow();

        // Act
        string[] stale = ValidationRulesTable.Read()
            .Where(row => !row.Field.StartsWith(ValidationRulesTable.PageRowPrefix, StringComparison.Ordinal) && !fields.ContainsKey(row.Field))
            .Select(row => row.Field)
            .ToArray();

        // Assert
        Assert.True(stale.Length == 0, "The table names fields that the flow does not have: " + string.Join(", ", stale));
    }

    /// <summary>Equivalence: the type of each row is the type of its field: text, date, whole number, boolean, an enum or an object.</summary>
    [Fact]
    public void Table_TypeOfEachRow_IsTheTypeOfItsField()
    {
        // Arrange
        Dictionary<string, Type> fields = FieldsOfTheFlow();

        // Act
        string[] wrong = ValidationRulesTable.Read()
            .Select(row => (Row: row, Expected: fields.TryGetValue(row.Field, out Type? field) ? TypeName(field) : null))
            .Where(pair => pair.Expected is not null && pair.Row.Type != pair.Expected)
            .Select(pair => $"{pair.Row.Field}: '{pair.Row.Type}', expected '{pair.Expected}'")
            .ToArray();

        // Assert
        Assert.True(wrong.Length == 0, string.Join(Environment.NewLine, wrong));
    }

    /// <summary>Negative: no cell is empty, and a text names its length with a number.</summary>
    [Fact]
    public void Table_EveryRow_FillsEveryColumnAndGivesATextItsLength()
    {
        // Act
        IReadOnlyList<ValidationRule> rows = ValidationRulesTable.Read();

        // Assert
        Assert.NotEmpty(rows);
        Assert.All(rows, row =>
        {
            Assert.DoesNotContain(row.Cells, cell => string.IsNullOrWhiteSpace(cell));
            if (row.Type == "text")
            {
                Assert.Matches("[0-9]", row.Length);
            }
        });
    }

    /// <summary>Negative: a code of the table that is no code of the API fails, so the table cannot promise a code that is never answered.</summary>
    [Fact]
    public void Table_EveryCode_IsACodeOfTheApi()
    {
        // Arrange
        HashSet<string> codes = KnownCodes();

        // Act
        string[] unknown = ValidationRulesTable.Read()
            .SelectMany(row => CodePattern().Matches(row.Code).Select(match => match.Groups[1].Value))
            .Where(code => !codes.Contains(code))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.True(unknown.Length == 0, "The table names codes that the API does not have: " + string.Join(", ", unknown));
    }

    /// <summary>Negative: a type or a member of the last column that does not exist fails, so the table names the code that holds each rule.</summary>
    [Fact]
    public void Table_EveryRuleInTheCode_Exists()
    {
        // Act
        string[] missing = ValidationRulesTable.Read()
            .Where(row => !row.Field.StartsWith(ValidationRulesTable.PageRowPrefix, StringComparison.Ordinal))
            .SelectMany(row => ValidationRulesTable.NamesOfCode(row.RuleInTheCode))
            .Where(name => !ValidationRulesTable.Exists(name, RuleAssemblies))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.True(missing.Length == 0, "The last column names code that does not exist: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Security: every request type of the flow refuses a field that it does not have (ASVS 15.3.3), and no request type of
    /// the flow holds a list without a largest size (API4:2023).
    /// </summary>
    [Fact]
    public void FlowTypes_WhenInspected_RefuseUnknownFieldsAndBoundTheirLists()
    {
        // Act
        List<Type> types = BodyTypesOfTheFlow();

        // Assert
        Assert.NotEmpty(types);
        Assert.All(types, type =>
        {
            JsonUnmappedMemberHandlingAttribute? handling = type.GetCustomAttribute<JsonUnmappedMemberHandlingAttribute>();
            Assert.True(handling?.UnmappedMemberHandling == JsonUnmappedMemberHandling.Disallow, $"{type.Name} lets an unknown field through.");
            Assert.All(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType)),
                property => Assert.True(HasMaxLength(type, property), $"{type.Name}.{property.Name} is a list without a largest size."));
        });
    }

    #endregion

    #region Reading the table and the code

    private static Dictionary<string, Type> FieldsOfTheFlow()
    {
        Dictionary<string, Type> fields = new(StringComparer.Ordinal);
        foreach ((Type? controller, string? action) in FlowActions)
        {
            MethodInfo method = controller.GetMethod(action) ?? throw new InvalidOperationException($"{controller.Name} has no action {action}.");
            foreach (ParameterInfo? parameter in method.GetParameters().Where(parameter => parameter.ParameterType != typeof(CancellationToken)))
            {
                if (parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
                {
                    AddFields(parameter.ParameterType, fields);
                }
                else if (parameter.GetCustomAttribute<FromQueryAttribute>() is not null)
                {
                    fields[$"{controller.Name}.{action}({parameter.Name})"] = parameter.ParameterType;
                }
                else
                {
                    Assert.Fail($"{controller.Name}.{action} reads {parameter.Name} from neither the body nor the query; give the table a way to name it.");
                }
            }
        }

        return fields;
    }

    private static void AddFields(Type type, Dictionary<string, Type> fields)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            fields[$"{type.Name}.{property.Name}"] = property.PropertyType;
            if (IsObject(property.PropertyType))
            {
                AddFields(property.PropertyType, fields);
            }
        }
    }

    private static List<Type> BodyTypesOfTheFlow()
    {
        List<Type> types = [];
        foreach ((Type? controller, string? action) in FlowActions)
        {
            foreach (ParameterInfo? parameter in controller.GetMethod(action)!.GetParameters().Where(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null))
            {
                AddObjectTypes(parameter.ParameterType, types);
            }
        }

        return types;
    }

    private static void AddObjectTypes(Type type, List<Type> types)
    {
        if (types.Contains(type))
        {
            return;
        }

        types.Add(type);
        foreach (PropertyInfo? property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => IsObject(property.PropertyType)))
        {
            AddObjectTypes(property.PropertyType, types);
        }
    }

    private static bool IsObject(Type type) =>
        type.IsClass && type != typeof(string) && type.Namespace?.StartsWith("NFHotel.", StringComparison.Ordinal) == true;

    private static bool HasMaxLength(Type type, PropertyInfo property) =>
        property.GetCustomAttribute<MaxLengthAttribute>() is not null
        || type.GetConstructors().SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.Name == property.Name && parameter.GetCustomAttribute<MaxLengthAttribute>() is not null);

    private static string TypeName(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying switch
        {
            _ when underlying == typeof(string) => "text",
            _ when underlying == typeof(DateOnly) => "date",
            _ when underlying == typeof(int) => "whole number",
            _ when underlying == typeof(bool) => "boolean",
            _ when underlying.IsEnum => $"enum `{underlying.Name}`",
            _ when IsObject(underlying) => $"object `{underlying.Name}`",
            _ => underlying.Name,
        };
    }

    private static HashSet<string> KnownCodes()
    {
        HashSet<string> codes = new(StringComparer.Ordinal);
        foreach (Type? type in new[] { typeof(ErrorCodes), typeof(ApiErrorCodes) })
        {
            AddConstants(type, codes);
        }

        return codes;
    }

    private static void AddConstants(Type type, HashSet<string> codes)
    {
        foreach (FieldInfo? field in type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral && field.FieldType == typeof(string)))
        {
            codes.Add((string)field.GetRawConstantValue()!);
        }

        foreach (Type nested in type.GetNestedTypes(BindingFlags.Public))
        {
            AddConstants(nested, codes);
        }
    }

    // A code of the API in back quotes, such as `booking.stay_too_long`.
    [GeneratedRegex("`([a-z_]+\\.[a-z_.]+)`", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex CodePattern();

    #endregion
}
