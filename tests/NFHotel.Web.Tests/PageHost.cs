using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace NFHotel.Web.Tests;

/// <summary>
/// Renders a page of the web front end with its view models, so a test can read the markup and click its buttons
/// (MIL-054). The view models are real, with their use cases faked; no browser, server or network is involved.
/// </summary>
internal sealed class PageHost : IAsyncDisposable
{
    private readonly BunitContext _context = new();

    public PageHost()
    {
        // A page that calls JavaScript gets an empty answer instead of an exception.
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Registers a view model or service the page injects.</summary>
    public PageHost With<TService>(TService service)
        where TService : class
    {
        _context.Services.AddSingleton(service);
        return this;
    }

    /// <summary>Signs a staff member in who holds the given policies, for the parts of a page behind <c>AuthorizeView</c>.</summary>
    public PageHost SignedInWith(params string[] policies)
    {
        var authorization = _context.AddAuthorization();
        authorization.SetAuthorized("rita");
        authorization.SetPolicies(policies);
        return this;
    }

    /// <summary>The navigation of the test host, to see where a page sent the user.</summary>
    public NavigationManager Navigation => _context.Services.GetRequiredService<NavigationManager>();

    /// <summary>Renders the page, which runs its first load.</summary>
    public IRenderedComponent<TPage> Render<TPage>(Action<ComponentParameterCollectionBuilder<TPage>>? parameters = null)
        where TPage : IComponent =>
        _context.Render(parameters);

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}

/// <summary>Finds the elements a user would find: a button by its text.</summary>
internal static class PageQueries
{
    /// <summary>The first button whose text is <paramref name="text"/>.</summary>
    public static IElement Button<TPage>(this IRenderedComponent<TPage> page, string text)
        where TPage : IComponent =>
        page.FindAll("button").First(button => button.TextContent.Trim() == text);

    /// <summary>Whether a button with the text is on the page.</summary>
    public static bool HasButton<TPage>(this IRenderedComponent<TPage> page, string text)
        where TPage : IComponent =>
        page.FindAll("button").Any(button => button.TextContent.Trim() == text);
}
