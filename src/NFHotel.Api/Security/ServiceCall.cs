namespace NFHotel.Api.Security;

/// <summary>
/// Marks a call that has shown the service token of the web application (MIL-087, ADR-0051 decision 4), so that only such
/// a call can name the address of its caller.
/// </summary>
public static class ServiceCall
{
    #region Fields

    // The key of the mark in the items of the call: an object of its own, so no other code can set the mark by a name.
    private static readonly object MarkKey = new();

    #endregion Fields

    #region Public methods

    /// <summary>Marks a call whose service token matched.</summary>
    /// <param name="context">The call.</param>
    public static void Mark(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Items[MarkKey] = true;
    }

    /// <summary>Tells whether a call has shown the service token.</summary>
    /// <param name="context">The call.</param>
    /// <returns><see langword="true"/> for a call that <see cref="Mark"/> marked.</returns>
    public static bool IsMarked(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.ContainsKey(MarkKey);
    }

    #endregion Public methods
}
