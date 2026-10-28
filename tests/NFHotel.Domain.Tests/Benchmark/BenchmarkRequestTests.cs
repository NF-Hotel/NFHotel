using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Tests.Benchmark;

/// <summary>The record of a started benchmark run and the states it may go through (ADR-0031).</summary>
public sealed class BenchmarkRequestTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset At = new(2026, 10, 26, 0, 9, 0, TimeSpan.Zero);

    #region Create

    /// <summary>Positive: a valid run is recorded as queued, with trimmed names and the time in UTC.</summary>
    [Fact]
    public void Create_ValidChoice_IsQueued()
    {
        // Act
        var request = BenchmarkRequest.Create(RunId, [" m1 ", "m2"], true, false, 2, " mona ", At.ToOffset(TimeSpan.FromHours(1)));

        // Assert
        Assert.Equal(BenchmarkRequest.Queued, request.State);
        Assert.Equal(["m1", "m2"], request.Models);
        Assert.Equal("mona", request.RequestedBy);
        Assert.Equal(TimeSpan.Zero, request.RequestedAt.Offset);
        Assert.Equal(request.RequestedAt, request.UpdatedAt);
        Assert.False(request.IsFinal);
    }

    /// <summary>Boundary: one and four models are accepted; none and five are refused.</summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void Create_ModelCount_FollowsTheLimit(int count, bool valid)
    {
        // Arrange
        var models = Enumerable.Range(1, count).Select(index => $"m{index}").ToArray();

        // Act
        var exception = Record.Exception(() => BenchmarkRequest.Create(RunId, models, true, false, 1, "mona", At));

        // Assert
        Assert.Equal(valid, exception is null);
        if (!valid)
        {
            Assert.Contains(BenchmarkRequestErrors.ModelsInvalid, Assert.IsType<DomainException>(exception).Codes);
        }
    }

    /// <summary>Boundary: one and five repetitions are accepted; zero and six are refused.</summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void Create_Runs_FollowsTheLimit(int runs, bool valid)
    {
        // Act
        var exception = Record.Exception(() => BenchmarkRequest.Create(RunId, ["m1"], true, false, runs, "mona", At));

        // Assert
        Assert.Equal(valid, exception is null);
    }

    /// <summary>Negative: every broken field is reported at once.</summary>
    [Fact]
    public void Create_EverythingWrong_ReportsEveryCode()
    {
        // Act
        var exception = Assert.Throws<DomainException>(() =>
            BenchmarkRequest.Create(Guid.Empty, ["m1", "m1"], false, false, 9, " ", At));

        // Assert
        string[] expected =
        [
            BenchmarkRequestErrors.RunIdRequired,
            BenchmarkRequestErrors.ModelsInvalid,
            BenchmarkRequestErrors.TasksInvalid,
            BenchmarkRequestErrors.RunsInvalid,
            BenchmarkRequestErrors.RequestedByInvalid,
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), exception.Codes.Order(StringComparer.Ordinal));
    }

    /// <summary>Edge: a model name longer than the limit is refused; one at the limit is accepted.</summary>
    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Create_ModelNameLength_FollowsTheLimit(int length, bool valid)
    {
        // Act
        var exception = Record.Exception(() => BenchmarkRequest.Create(RunId, [new string('m', length)], true, false, 1, "mona", At));

        // Assert
        Assert.Equal(valid, exception is null);
    }

    #endregion

    #region States

    /// <summary>State: every allowed change is taken and moves the time of the last change.</summary>
    [Theory]
    [InlineData(BenchmarkRequest.Queued, BenchmarkRequest.Running)]
    [InlineData(BenchmarkRequest.Queued, BenchmarkRequest.Done)]
    [InlineData(BenchmarkRequest.Queued, BenchmarkRequest.Failed)]
    [InlineData(BenchmarkRequest.Queued, BenchmarkRequest.Interrupted)]
    [InlineData(BenchmarkRequest.Running, BenchmarkRequest.Done)]
    [InlineData(BenchmarkRequest.Running, BenchmarkRequest.Failed)]
    [InlineData(BenchmarkRequest.Running, BenchmarkRequest.Interrupted)]
    [InlineData(BenchmarkRequest.Interrupted, BenchmarkRequest.Done)]
    public void ChangeState_AllowedChange_IsTaken(string from, string to)
    {
        // Arrange
        var request = InState(from);
        var later = At.AddMinutes(10);

        // Act
        var changed = request.ChangeState(to, null, later);

        // Assert
        Assert.True(changed);
        Assert.Equal(to, request.State);
        Assert.Equal(later, request.UpdatedAt);
    }

    /// <summary>State: a final state, or a step back, is refused and the record stays as it was.</summary>
    [Theory]
    [InlineData(BenchmarkRequest.Running, BenchmarkRequest.Queued)]
    [InlineData(BenchmarkRequest.Done, BenchmarkRequest.Running)]
    [InlineData(BenchmarkRequest.Done, BenchmarkRequest.Interrupted)]
    [InlineData(BenchmarkRequest.Failed, BenchmarkRequest.Done)]
    [InlineData(BenchmarkRequest.Interrupted, BenchmarkRequest.Running)]
    [InlineData(BenchmarkRequest.Interrupted, BenchmarkRequest.Failed)]
    [InlineData(BenchmarkRequest.Done, BenchmarkRequest.Failed)]
    public void ChangeState_RefusedChange_Throws(string from, string to)
    {
        // Arrange
        var request = InState(from);

        // Act
        var exception = Assert.Throws<DomainException>(() => request.ChangeState(to, null, At.AddMinutes(10)));

        // Assert
        Assert.Equal(BenchmarkRequestErrors.StateChangeRefused, exception.Code);
        Assert.Equal(from, request.State);
    }

    /// <summary>Equivalence: the same state again changes nothing, also on a final state.</summary>
    [Theory]
    [InlineData(BenchmarkRequest.Queued)]
    [InlineData(BenchmarkRequest.Interrupted)]
    public void ChangeState_SameState_ChangesNothing(string state)
    {
        // Arrange
        var request = InState(state);
        var before = request.UpdatedAt;

        // Act
        var changed = request.ChangeState(state, "ignored", At.AddHours(1));

        // Assert
        Assert.False(changed);
        Assert.Equal(before, request.UpdatedAt);
    }

    /// <summary>Negative: an unknown state is refused.</summary>
    [Theory]
    [InlineData("paused")]
    [InlineData("")]
    [InlineData(null)]
    public void ChangeState_UnknownState_Throws(string? state)
    {
        // Arrange
        var request = InState(BenchmarkRequest.Queued);

        // Act
        var exception = Assert.Throws<DomainException>(() => request.ChangeState(state!, null, At));

        // Assert
        Assert.Equal(BenchmarkRequestErrors.StateInvalid, exception.Code);
    }

    /// <summary>Boundary: an error at the limit is kept and a longer one refused; a blank error is kept as none.</summary>
    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void ChangeState_ErrorLength_FollowsTheLimit(int length, bool valid)
    {
        // Arrange
        var request = InState(BenchmarkRequest.Running);

        // Act
        var exception = Record.Exception(() => request.ChangeState(BenchmarkRequest.Failed, new string('e', length), At));

        // Assert
        Assert.Equal(valid, exception is null);
        Assert.Equal(valid ? BenchmarkRequest.Failed : BenchmarkRequest.Running, request.State);
    }

    /// <summary>Edge: a blank error is stored as none; an interrupted run is not final, since a stored result can still prove it done.</summary>
    [Fact]
    public void ChangeState_BlankError_IsStoredAsNone()
    {
        // Arrange
        var request = InState(BenchmarkRequest.Queued);

        // Act
        request.ChangeState(BenchmarkRequest.Interrupted, "   ", At);

        // Assert
        Assert.Null(request.Error);
        Assert.False(request.IsFinal);
    }

    #endregion

    private static BenchmarkRequest InState(string state)
    {
        var request = BenchmarkRequest.Create(RunId, ["m1"], true, false, 1, "mona", At);
        if (state == BenchmarkRequest.Queued)
        {
            return request;
        }

        if (state is BenchmarkRequest.Done or BenchmarkRequest.Failed or BenchmarkRequest.Interrupted)
        {
            request.ChangeState(BenchmarkRequest.Running, null, At);
        }

        request.ChangeState(state, null, At);
        return request;
    }
}
