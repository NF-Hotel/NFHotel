using System.Reflection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

using NFHotel.Api.Security;

namespace NFHotel.Api.Tests;

/// <summary>
/// No endpoint can be added without a decision about who may call it (ADR-0005): each action of each controller either
/// carries <see cref="RequiresPermissionAttribute"/> or is on the short list of public endpoints below.
/// </summary>
public sealed class EndpointProtectionTests
{
    /// <summary>The actions that need only the token of the web application: the public booking flow and the sign-in itself.</summary>
    private static readonly string[] PublicActions =
    [
        "BookingsController.GetByIdAsync",
        "BookingsController.GetAvailableRoomsAsync",
        "BookingsController.CreateAsync",
        "AuthController.LoginAsync",
    ];

    #region Protection

    /// <summary>Every controller action is either on the public list or requires a permission.</summary>
    [Fact]
    public void Controllers_EveryAction_RequiresAPermissionOrIsPublic()
    {
        // Arrange
        var unprotected = ActionsOfTheApi()
            .Where(action => action.GetCustomAttribute<RequiresPermissionAttribute>() is null)
            .Select(Describe)
            .Except(PublicActions)
            .ToArray();

        // Act and assert
        Assert.True(unprotected.Length == 0, $"Actions without a permission and not on the public list: {string.Join(", ", unprotected)}");
    }

    /// <summary>The public list names only actions that exist, so a renamed action cannot hide behind an old entry.</summary>
    [Fact]
    public void PublicActions_EveryEntry_NamesAnExistingAction()
    {
        // Arrange
        var existing = ActionsOfTheApi().Select(Describe).ToHashSet();

        // Act
        var missing = PublicActions.Where(entry => !existing.Contains(entry)).ToArray();

        // Assert
        Assert.True(missing.Length == 0, $"Public entries without an action: {string.Join(", ", missing)}");
    }

    /// <summary>An action on the public list does not also carry a permission, so the list says what is true.</summary>
    [Fact]
    public void PublicActions_EveryEntry_HasNoPermission()
    {
        // Arrange
        var both = ActionsOfTheApi()
            .Where(action => action.GetCustomAttribute<RequiresPermissionAttribute>() is not null)
            .Select(Describe)
            .Intersect(PublicActions)
            .ToArray();

        // Act and assert
        Assert.True(both.Length == 0, $"Public and protected at the same time: {string.Join(", ", both)}");
    }

    #endregion

    #region Helpers

    private static IEnumerable<MethodInfo> ActionsOfTheApi() =>
        typeof(Program).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes().Any(attribute => attribute is HttpMethodAttribute));

    private static string Describe(MethodInfo action) => $"{action.DeclaringType!.Name}.{action.Name}";

    #endregion
}