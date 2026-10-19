namespace NFHotel.Web.Common;

/// <summary>
/// The severity of a message shown in <c>AlertBox</c>.
/// </summary>
/// <remarks>
/// A pure display concept and therefore the responsibility of the Web layer. The Application layer returns
/// error codes, not colours — the translation happens here and in <see cref="ErrorMessages"/>.
/// </remarks>
public enum AlertKind
{
    /// <summary>Neutral information. Requires no action.</summary>
    Info = 0,

    /// <summary>Handlingen lykkedes.</summary>
    Ok = 1,

    /// <summary>The action failed, or data is invalid.</summary>
    Error = 2
}
