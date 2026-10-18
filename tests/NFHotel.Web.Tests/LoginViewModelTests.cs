using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Login;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the login page: the session on success, a translated refusal otherwise (ADR-0005).</summary>
public sealed class LoginViewModelTests
{
    private static readonly DateTimeOffset Later = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    #region Result

    /// <summary>Positive: a success returns the session and leaves no errors.</summary>
    [Fact]
    public async Task SignInAsync_Success_ReturnsTheSessionWithoutErrors()
    {
        // Arrange
        var session = new StaffSessionDto("t", Later, "rita", StaffRole.Manager);
        var viewModel = Build(Result<StaffSessionDto>.Success(session), out _);

        // Act
        var result = await viewModel.SignInAsync("rita", "pw", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(session, result);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: a refusal returns nothing and keeps the translated text, never the code.</summary>
    [Theory]
    [InlineData(ErrorCodes.Staff.InvalidCredentials, "The user name or the password is wrong.")]
    [InlineData(ErrorCodes.Staff.TooManyAttempts, "Too many failed attempts. Wait fifteen minutes and try again.")]
    [InlineData(ErrorCodes.Api.Unavailable, "We couldn't reach the hotel system. Please try again in a moment.")]
    public async Task SignInAsync_Refused_ReturnsNullWithTheTranslatedText(string code, string text)
    {
        // Arrange
        var viewModel = Build(Result<StaffSessionDto>.Failure(code), out _);

        // Act
        var result = await viewModel.SignInAsync("rita", "pw", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
        Assert.Equal([text], viewModel.Errors);
    }

    /// <summary>State transition: a success after a refusal clears the errors.</summary>
    [Fact]
    public async Task SignInAsync_SuccessAfterRefusal_ClearsTheErrors()
    {
        // Arrange
        var viewModel = Build(Result<StaffSessionDto>.Failure(ErrorCodes.Staff.InvalidCredentials), out var service);
        await viewModel.SignInAsync("rita", "wrong", TestContext.Current.CancellationToken);
        service.SignInAsync(Arg.Any<SignInRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<StaffSessionDto>.Success(new StaffSessionDto("t", Later, "rita", StaffRole.Manager)));

        // Act
        await viewModel.SignInAsync("rita", "right", TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(viewModel.Errors);
    }

    #endregion

    #region Request

    /// <summary>Edge: a missing name or password is sent as empty text, so the use case answers with the same refusal as for a wrong one.</summary>
    [Fact]
    public async Task SignInAsync_MissingNameAndPassword_SendsEmptyText()
    {
        // Arrange
        var viewModel = Build(Result<StaffSessionDto>.Failure(ErrorCodes.Staff.InvalidCredentials), out var service);

        // Act
        await viewModel.SignInAsync(null, null, TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).SignInAsync(new SignInRequest(string.Empty, string.Empty), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static LoginViewModel Build(Result<StaffSessionDto> answer, out IStaffAuthService service)
    {
        var auth = Substitute.For<IStaffAuthService>();
        auth.SignInAsync(Arg.Any<SignInRequest>(), Arg.Any<CancellationToken>()).Returns(answer);
        service = auth;

        var runner = Substitute.For<IUseCaseRunner<IStaffAuthService>>();
        runner.RunAsync(
                Arg.Any<Func<IStaffAuthService, CancellationToken, Task<Result<StaffSessionDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IStaffAuthService, CancellationToken, Task<Result<StaffSessionDto>>>>()(auth, call.Arg<CancellationToken>()));
        return new LoginViewModel(runner);
    }

    #endregion
}