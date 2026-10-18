using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// Hosts the real web application (its pipeline, cookie, policies and pages) with a substitute for the sign-in use case,
/// so a test signs in without an API. Any other call to the API fails at once: the address is not reachable.
/// </summary>
public partial class WebFactory : WebApplicationFactory<Program>
{
    /// <summary>The password that the substitute accepts for every user name.</summary>
    public const string Password = "correct horse battery staple";

    /// <summary>The token the substitute hands out, so a test can look for it in a cookie or a page.</summary>
    public const string StaffToken = "secret-staff-token-that-must-not-leak-0123456789";

    /// <summary>The user names that the substitute knows, and their roles.</summary>
    public IReadOnlyDictionary<string, StaffRole> Accounts { get; } = new Dictionary<string, StaffRole>
    {
        ["rita"] = StaffRole.Receptionist,
        ["mona"] = StaffRole.Manager,
        ["hilda"] = StaffRole.Housekeeping,
    };

    /// <summary>Creates a client that keeps cookies and does not follow redirects, so a test sees every 302.</summary>
    /// <returns>The client.</returns>
    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>Opens the login page, fills the form and posts it.</summary>
    /// <param name="browser">The client that holds the cookies.</param>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="returnUrl">The address to return to, or <c>null</c>.</param>
    /// <returns>The answer to the post.</returns>
    public static async Task<HttpResponseMessage> SignInAsync(HttpClient browser, string userName, string password, string? returnUrl = null)
    {
        ArgumentNullException.ThrowIfNull(browser);

        var path = returnUrl is null ? "/admin/login" : $"/admin/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        var html = await browser.GetStringAsync(path, TestContext.Current.CancellationToken);
        var fields = HiddenFields(html);
        fields[InputName(html, "login-user")] = userName;
        fields[InputName(html, "login-password")] = password;
        return await browser.PostAsync(path, new FormUrlEncodedContent(fields), TestContext.Current.CancellationToken);
    }

    /// <summary>Reads the hidden fields of the first form, such as the antiforgery token and the form name.</summary>
    /// <param name="html">The page.</param>
    /// <returns>The names and values.</returns>
    public static Dictionary<string, string> HiddenFields(string html)
    {
        var fields = new Dictionary<string, string>();
        foreach (Match input in InputTag().Matches(html))
        {
            var tag = input.Value;
            if (tag.Contains("type=\"hidden\"", StringComparison.Ordinal))
            {
                fields[Attribute(tag, "name")] = WebUtility.HtmlDecode(Attribute(tag, "value"));
            }
        }

        return fields;
    }

    /// <summary>Finds the name attribute of the input with an id.</summary>
    /// <param name="html">The page.</param>
    /// <param name="id">The id.</param>
    /// <returns>The name.</returns>
    public static string InputName(string html, string id)
    {
        foreach (Match input in InputTag().Matches(html))
        {
            if (input.Value.Contains($"id=\"{id}\"", StringComparison.Ordinal))
            {
                return Attribute(input.Value, "name");
            }
        }

        throw new InvalidOperationException($"No input with the id '{id}' on the page.");
    }

    /// <summary>Extra settings for the host; a derived factory adds its own.</summary>
    /// <returns>Pairs of setting name and value.</returns>
    protected virtual IEnumerable<KeyValuePair<string, string>> ExtraSettings() => [];

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("Api:BaseUrl", "http://api.invalid/");
        builder.UseSetting("Api:Token", "web-test-token-0123456789-web-test-token");
        builder.UseSetting("HotelTime:TimeZoneId", "Europe/Copenhagen");
        foreach (var (name, value) in ExtraSettings())
        {
            builder.UseSetting(name, value);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStaffAuthService>();
            services.AddScoped(_ => SignInSubstitute());
        });
    }

    private IStaffAuthService SignInSubstitute()
    {
        var service = Substitute.For<IStaffAuthService>();
        service.SignInAsync(Arg.Any<SignInRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<SignInRequest>();
            return Accounts.TryGetValue(request.UserName.Trim().ToLowerInvariant(), out var role) && request.Password == Password
                ? Result<StaffSessionDto>.Success(new StaffSessionDto(StaffToken, new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero), request.UserName.Trim().ToLowerInvariant(), role))
                : Result<StaffSessionDto>.Failure(ErrorCodes.Staff.InvalidCredentials);
        });
        return service;
    }

    private static string Attribute(string tag, string name)
    {
        var match = Regex.Match(tag, $"""\b{name}="([^"]*)"|\b{name}=([^\s>]+)""");
        return match.Success ? (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value) : string.Empty;
    }

    [GeneratedRegex("<input[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex InputTag();
}