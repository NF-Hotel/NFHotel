namespace NFHotel.Web.Components.Layout;

/// <summary>
/// Where each admin route sits in the menu: the group and the name the top bar shows as the breadcrumb.
/// One place, so AdminNav and AdminTopbar cannot drift apart.
/// </summary>
public static class AdminRoutes
{
    /// <summary>The front desk's screens: today, bookings and guests.</summary>
    public const string FrontDesk = "Front desk";

    /// <summary>The rooms and their cleaning.</summary>
    public const string Housekeeping = "Housekeeping";

    /// <summary>Revenue and the prices.</summary>
    public const string Revenue = "Revenue & pricing";

    /// <summary>The market analysis and the benchmark of the local models.</summary>
    public const string Ai = "AI & analysis";

    /// <summary>The staff accounts and their audit trail.</summary>
    public const string Administration = "Administration";

    /// <summary>The signed-in user's own account.</summary>
    public const string Account = "Account";

    /// <summary>A place in the menu: the group and the name of the page.</summary>
    public sealed record Crumb(string Group, string Title);

    /// <summary>The breadcrumb for a path, for example <c>/admin/pricing/rates</c>.</summary>
    public static Crumb For(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        string clean = path.Split('?', 2)[0].TrimEnd('/').ToLowerInvariant();
        if (clean.Length == 0)
        {
            clean = "/admin";
        }

        return clean switch
        {
            "/admin" => new(FrontDesk, "Dashboard"),
            "/admin/bookings" => new(FrontDesk, "Bookings"),
            "/admin/guests" => new(FrontDesk, "Guests"),
            "/admin/rooms" => new(Housekeeping, "Rooms"),
            "/admin/revenue" => new(Revenue, "Revenue"),
            "/admin/market-analysis" => new(Ai, "Market analysis"),
            "/admin/account/password" => new(Account, "Change password"),
            "/admin/denied" => new(Administration, "Not permitted"),
            _ when clean.StartsWith("/admin/pricing", StringComparison.Ordinal) => new(Revenue, "Pricing"),
            _ when clean.StartsWith("/admin/benchmark", StringComparison.Ordinal) => new(Ai, "Benchmark"),
            _ when clean.StartsWith("/admin/staff", StringComparison.Ordinal) => new(Administration, "Staff & audit"),
            _ => new(Administration, "Administration"),
        };
    }

    /// <summary>True when the current path is the page, or sits under it.</summary>
    public static bool IsUnder(string path, string prefix)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(prefix);

        string clean = path.Split('?', 2)[0].TrimEnd('/');
        return clean.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || clean.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }
}
