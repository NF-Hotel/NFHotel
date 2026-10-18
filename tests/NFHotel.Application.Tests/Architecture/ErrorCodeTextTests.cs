using System.Reflection;

using NFHotel.Application.Errors;

namespace NFHotel.Application.Tests.Architecture;

/// <summary>Every error code of the application has a screen text, so the user never sees a bare code.</summary>
public sealed class ErrorCodeTextTests
{
    #region Screen texts

    /// <summary>Each code in <c>ErrorCodes</c> is mapped in <c>ErrorMessages</c> of the web project (source scan).</summary>
    [Fact]
    public void ErrorCodes_EveryConstant_HasATextInErrorMessages()
    {
        // Arrange
        var messages = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "src", "NFHotel.Web", "Common", "ErrorMessages.cs"));
        var missing = typeof(ErrorCodes)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(group => group
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .Select(field => $"ErrorCodes.{group.Name}.{field.Name}"))
            .Where(reference => !messages.Contains(reference, StringComparison.Ordinal))
            .ToArray();

        // Act and assert
        Assert.True(missing.Length == 0, $"Error codes without a screen text: {string.Join(", ", missing)}");
    }

    #endregion
}