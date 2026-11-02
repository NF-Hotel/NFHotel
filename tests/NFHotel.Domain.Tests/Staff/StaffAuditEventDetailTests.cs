using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Staff;

/// <summary>The detail text of an audit event, used for the old and the new value of a rate (MIL-055).</summary>
public sealed class StaffAuditEventDetailTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Positive: a detail is kept as given.</summary>
    [Fact]
    public void Create_WithDetail_KeepsIt()
    {
        // Arrange and act
        var auditEvent = StaffAuditEvent.Create(At, StaffAuditAction.RoomRateChanged, "Success", detail: "Single: 80.00 -> 90.00");

        // Assert
        Assert.Equal("Single: 80.00 -> 90.00", auditEvent.Detail);
    }

    /// <summary>Negative: without a detail the event has none.</summary>
    [Fact]
    public void Create_WithoutDetail_HasNone()
    {
        // Arrange and act
        var auditEvent = StaffAuditEvent.Create(At, StaffAuditAction.SignedIn, "Success");

        // Assert
        Assert.Null(auditEvent.Detail);
    }

    /// <summary>Boundary: a detail of 200 characters is kept, one of 201 is cut to 200.</summary>
    [Fact]
    public void Create_DetailLength_KeepsTheLimitAndCutsOneMore()
    {
        // Arrange and act
        var kept = StaffAuditEvent.Create(At, StaffAuditAction.RoomRateChanged, "Success", detail: new string('d', StaffAuditEvent.MaxDetailLength));
        var cut = StaffAuditEvent.Create(At, StaffAuditAction.RoomRateChanged, "Success", detail: new string('d', StaffAuditEvent.MaxDetailLength + 1));

        // Assert
        Assert.Equal(StaffAuditEvent.MaxDetailLength, kept.Detail!.Length);
        Assert.Equal(StaffAuditEvent.MaxDetailLength, cut.Detail!.Length);
    }
}
