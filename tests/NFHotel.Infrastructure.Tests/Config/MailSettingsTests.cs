using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;
using NFHotel.TestSupport;

namespace NFHotel.Infrastructure.Tests.Config;

/// <summary>
/// The settings of the mail as the start of the API reads them (MIL-088 criteria 6 and 8, ADR-0052 decision 8 and ADR-0053
/// decision 2): a wrong value stops the start with the name of its setting, the verification on without a relay stops
/// it, and no message holds the password.
/// </summary>
public sealed class MailSettingsTests
{
    private const string Password = "relay-password-of-the-test";

    #region Read

    /// <summary>Positive: without the sections the relay has its defaults and the verification is off.</summary>
    [Fact]
    public void AddInfrastructure_NoMailSettings_HasTheDefaultsAndTheVerificationOff()
    {
        // Act
        using ServiceProvider provider = Build(new());

        // Assert
        SmtpOptions smtp = provider.GetRequiredService<SmtpOptions>();
        IBookingVerificationSettings verification = provider.GetRequiredService<IBookingVerificationSettings>();
        Assert.Null(smtp.Host);
        Assert.Equal(SmtpSecurity.SslOnConnect, smtp.Security);
        Assert.Equal("NF Hotel", smtp.FromName);
        Assert.False(verification.Enabled);
        Assert.Null(verification.SiteAddress);
        Assert.Equal(400, verification.MaxMailsPerDay);
    }

    /// <summary>Positive (ADR-0053 decision 8): the settings of production are read as they are written in the env file.</summary>
    [Fact]
    public void AddInfrastructure_SettingsOfProduction_AreRead()
    {
        // Act
        using ServiceProvider provider = Build(Production());

        // Assert
        SmtpOptions smtp = provider.GetRequiredService<SmtpOptions>();
        IBookingVerificationSettings verification = provider.GetRequiredService<IBookingVerificationSettings>();
        Assert.Equal("smtp.example.com", smtp.Host);
        Assert.Equal(465, smtp.Port);
        Assert.Equal(SmtpSecurity.SslOnConnect, smtp.Security);
        Assert.Equal("mail-relay", smtp.ForwarderHost);
        Assert.Equal(1465, smtp.ForwarderPort);
        Assert.Null(smtp.CertificateThumbprint);
        Assert.True(verification.Enabled);
        Assert.Equal(new Uri("https://hotel.example.com/"), verification.SiteAddress);
        Assert.Equal(400, verification.MaxMailsPerDay);
    }

    /// <summary>Positive: the two modes of encryption are read without letter case, and a pin of 64 lower-case hex digits is kept.</summary>
    [Fact]
    public void AddInfrastructure_StartTlsAndAPin_AreRead()
    {
        // Act
        using ServiceProvider provider = Build(new()
        {
            ["Smtp:Security"] = "starttls",
            ["Smtp:CertificateThumbprint"] = new string('a', 64),
            ["Smtp:MaxMailsPerDay"] = "50",
        });

        // Assert
        SmtpOptions smtp = provider.GetRequiredService<SmtpOptions>();
        Assert.Equal(SmtpSecurity.StartTls, smtp.Security);
        Assert.Equal(new string('a', 64), smtp.CertificateThumbprint);
        Assert.Equal(50, provider.GetRequiredService<IBookingVerificationSettings>().MaxMailsPerDay);
    }

    #endregion

    #region Refused

    /// <summary>
    /// Security (criterion 6, ADR-0053 decision 2): a value that is not allowed stops the start, and the message names the
    /// setting.
    /// </summary>
    /// <param name="key">The setting.</param>
    /// <param name="value">Its value.</param>
    [Theory]
    [InlineData("Smtp:Security", "None")]
    [InlineData("Smtp:Security", "0")]
    [InlineData("Smtp:Security", "Auto")]
    [InlineData("Smtp:Port", "0")]
    [InlineData("Smtp:Port", "65536")]
    [InlineData("Smtp:Port", "-25")]
    [InlineData("Smtp:Port", "25a")]
    [InlineData("Smtp:Forwarder", "mail-relay")]
    [InlineData("Smtp:Forwarder", ":1465")]
    [InlineData("Smtp:Forwarder", "mail-relay:")]
    [InlineData("Smtp:Forwarder", "mail-relay:70000")]
    [InlineData("Smtp:CertificateThumbprint", "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789")]
    [InlineData("Smtp:CertificateThumbprint", "abcdef")]
    [InlineData("Smtp:CertificateThumbprint", "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("Smtp:MaxMailsPerDay", "0")]
    [InlineData("Smtp:MaxMailsPerDay", "many")]
    [InlineData("BookingVerification:Enabled", "yes")]
    [InlineData("BookingVerification:SiteAddress", "hotel.example.com")]
    public void AddInfrastructure_ValueNotAllowed_StopsTheStartAndNamesTheSetting(string key, string value)
    {
        // Act
        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => Build(new() { [key] = value }).Dispose());

        // Assert
        Assert.Contains($"'{key}'", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Negative (criterion 8, ADR-0052 decision 8): with the verification on, each setting of the relay that is missing
    /// stops the start with its name, and the message never holds the password.
    /// </summary>
    /// <param name="key">The setting that is missing.</param>
    [Theory]
    [InlineData("Smtp:Host")]
    [InlineData("Smtp:Port")]
    [InlineData("Smtp:UserName")]
    [InlineData("Smtp:Password")]
    [InlineData("Smtp:FromAddress")]
    [InlineData("BookingVerification:SiteAddress")]
    public void AddInfrastructure_VerificationOnWithASettingMissing_StopsTheStartAndNamesIt(string key)
    {
        // Arrange
        Dictionary<string, string?> settings = Production();
        settings.Remove(key);

        // Act
        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => Build(settings).Dispose());

        // Assert
        Assert.Contains($"'{key}' must be set", refused.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, refused.Message, StringComparison.Ordinal);
    }

    /// <summary>Security (ADR-0052 decision 2): with the verification on, a site address that is not https stops the start.</summary>
    [Fact]
    public void AddInfrastructure_VerificationOnWithAnHttpSiteAddress_StopsTheStart()
    {
        // Arrange
        Dictionary<string, string?> settings = Production();
        settings["BookingVerification:SiteAddress"] = "http://hotel.example.com/";

        // Act
        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(() => Build(settings).Dispose());

        // Assert
        Assert.Contains("must be an https address", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Positive (ADR-0052 decision 8): a relay without the verification starts, as the state between the two deploys of
    /// task 9.
    /// </summary>
    [Fact]
    public void AddInfrastructure_RelayWithTheVerificationOff_Starts()
    {
        // Arrange
        Dictionary<string, string?> settings = Production();
        settings["BookingVerification:Enabled"] = "false";
        settings.Remove("BookingVerification:SiteAddress");

        // Act
        using ServiceProvider provider = Build(settings);

        // Assert
        Assert.False(provider.GetRequiredService<IBookingVerificationSettings>().Enabled);
    }

    #endregion

    #region Helpers

    // The settings of ADR-0053 decision 8 with the values of an example.
    private static Dictionary<string, string?> Production() => new()
    {
        ["Smtp:Host"] = "smtp.example.com",
        ["Smtp:Port"] = "465",
        ["Smtp:Security"] = "SslOnConnect",
        ["Smtp:UserName"] = "hotel@example.com",
        ["Smtp:Password"] = Password,
        ["Smtp:FromAddress"] = "hotel@example.com",
        ["Smtp:Forwarder"] = "mail-relay:1465",
        ["BookingVerification:Enabled"] = "true",
        ["BookingVerification:SiteAddress"] = "https://hotel.example.com/",
    };

    private static ServiceProvider Build(Dictionary<string, string?> mail)
    {
        Dictionary<string, string?> settings = new()
        {
            ["ConnectionStrings:HotelDatabase"] = "Host=localhost;Database=model-only",
            ["Encryption:PassportKey"] = Convert.ToBase64String(new byte[32]),
            ["Staff:Token:SigningKey"] = "staff-signing-key-0123456789-staff-signing-key",
        };
        foreach ((string key, string? value) in mail)
        {
            settings[key] = value;
        }

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IClock>(new FakeClock());
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        return services.BuildServiceProvider(validateScopes: true);
    }

    #endregion
}
