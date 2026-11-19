using Microsoft.AspNetCore.Mvc;

namespace NFHotel.Api.Security;

/// <summary>Puts an action under the limits per caller address of a kind (MIL-087, ADR-0051 decision 2).</summary>
public sealed class SenderLimitAttribute : TypeFilterAttribute
{
    #region Constructors

    /// <summary>Puts the action under the limits of <paramref name="kind"/>.</summary>
    /// <param name="kind">What the limits count.</param>
    public SenderLimitAttribute(SenderLimitKind kind)
        : base(typeof(SenderLimitFilter))
    {
        Kind = kind;
        Arguments = [kind];
    }

    #endregion Constructors

    #region Properties

    /// <summary>What the limits count.</summary>
    public SenderLimitKind Kind { get; }

    #endregion Properties
}
