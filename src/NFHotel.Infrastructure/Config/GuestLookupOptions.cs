namespace NFHotel.Infrastructure.Config;

/// <summary>
/// The limits on the tries of the guest sign-in (ADR-0049 decision 6), read from the section <c>GuestLookup</c>. The
/// defaults are the numbers of the decision; a value below one stops the start-up, so a typing mistake cannot switch a
/// limit off.
/// </summary>
public sealed class GuestLookupOptions
{
    #region Fields

    /// <summary>The configuration section.</summary>
    public const string SectionName = "GuestLookup";

    #endregion Fields

    #region Properties

    /// <summary>Failed tries of one reference in a window (<c>GuestLookup:MaxFailuresPerReference</c>).</summary>
    public int MaxFailuresPerReference { get; init; } = 5;

    /// <summary>
    /// Failed tries from one caller address in a window (<c>GuestLookup:MaxFailuresPerAddress</c>): four times the limit
    /// per reference, the factor of the staff sign-in since ADR-0015.
    /// </summary>
    public int MaxFailuresPerAddress { get; init; } = 20;

    /// <summary>The length of a window in minutes (<c>GuestLookup:WindowMinutes</c>); it starts at the first failed try.</summary>
    public int WindowMinutes { get; init; } = 15;

    /// <summary>The most windows that each of the two stores holds (<c>GuestLookup:MaxTrackedKeys</c>).</summary>
    public int MaxTrackedKeys { get; init; } = 10_000;

    #endregion Properties
}
