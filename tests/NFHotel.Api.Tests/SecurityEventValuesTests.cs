using System.Reflection;

using Microsoft.Extensions.Logging;

using NFHotel.Api.Security;

namespace NFHotel.Api.Tests;

/// <summary>
/// The values that a security event holds (MIL-087; MIL-089, ADR-0051 decision 6): the address of the caller and words of
/// the code, never an email, whole, masked or hashed, nor another value that a guest typed.
/// </summary>
public sealed class SecurityEventValuesTests
{
    /// <summary>
    /// Security (ADR-0051 decision 6 since MIL-089): no message of the log of the security events has a field for an
    /// email, so no caller can write one into the log through an event.
    /// </summary>
    [Fact]
    public void Events_EveryLogMessage_TakesNoEmail()
    {
        // Arrange
        MethodInfo[] events =
        [
            .. typeof(SecurityEvents).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.GetCustomAttribute<LoggerMessageAttribute>() is not null),
        ];

        // Act
        string[] withEmail = [.. events.Where(TakesAnEmail).Select(method => method.Name)];

        // Assert
        Assert.Equal(8, events.Length);
        Assert.Empty(withEmail);
    }

    // A message with a field of that name, or a parameter of that name. Every method of the list carries the attribute,
    // and a parameter of compiled code has a name; a missing one would count as an email, so the test cannot pass on it.
    private static bool TakesAnEmail(MethodInfo method) =>
        method.GetCustomAttribute<LoggerMessageAttribute>() is not { } attribute
        || attribute.Message.Contains("email", StringComparison.OrdinalIgnoreCase)
        || method.GetParameters().Any(parameter => parameter.Name?.Contains("email", StringComparison.OrdinalIgnoreCase) != false);
}
