using FluentAssertions;
using Lexichord.Modules.StatusBar.ViewModels;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.StatusBar;

/// <summary>
/// Unit tests for uptime formatting in StatusBarViewModel.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the FormatUptime static method correctly
/// formats various duration ranges:
/// - Seconds only
/// - Minutes and seconds
/// - Hours, minutes, seconds
/// - Days included
/// </remarks>
public class UptimeFormattingTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, "0:00:00")]
    [InlineData(0, 0, 0, 30, "0:00:30")]
    [InlineData(0, 0, 5, 30, "0:05:30")]
    [InlineData(0, 0, 59, 59, "0:59:59")]
    public void FormatUptime_UnderOneHour_FormatsCorrectly(int days, int hours, int minutes, int seconds, string expected)
    {
        // Arrange
        var uptime = new TimeSpan(days, hours, minutes, seconds);

        // Act
        var result = StatusBarViewModel.FormatUptime(uptime);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1, 0, 0, "1:00:00")]
    [InlineData(0, 1, 30, 45, "1:30:45")]
    [InlineData(0, 12, 0, 0, "12:00:00")]
    [InlineData(0, 23, 59, 59, "23:59:59")]
    public void FormatUptime_UnderOneDay_FormatsWithHours(int days, int hours, int minutes, int seconds, string expected)
    {
        // Arrange
        var uptime = new TimeSpan(days, hours, minutes, seconds);

        // Act
        var result = StatusBarViewModel.FormatUptime(uptime);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0, 0, 0, "1d 0:00:00")]
    [InlineData(1, 2, 30, 45, "1d 2:30:45")]
    [InlineData(7, 12, 30, 0, "7d 12:30:00")]
    [InlineData(30, 0, 0, 1, "30d 0:00:01")]
    public void FormatUptime_DaysIncluded_FormatsWithDayPrefix(int days, int hours, int minutes, int seconds, string expected)
    {
        // Arrange
        var uptime = new TimeSpan(days, hours, minutes, seconds);

        // Act
        var result = StatusBarViewModel.FormatUptime(uptime);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatUptime_ZeroTimeSpan_ReturnsZeroFormat()
    {
        // Arrange
        var uptime = TimeSpan.Zero;

        // Act
        var result = StatusBarViewModel.FormatUptime(uptime);

        // Assert
        result.Should().Be("0:00:00");
    }

    [Fact]
    public void FormatUptime_LargeDuration_HandlesCorrectly()
    {
        // Arrange - 100 days, 5 hours, 30 minutes, 15 seconds
        var uptime = new TimeSpan(100, 5, 30, 15);

        // Act
        var result = StatusBarViewModel.FormatUptime(uptime);

        // Assert
        result.Should().Be("100d 5:30:15");
    }

    [Fact]
    public void FormatUptime_PadsMinutesAndSeconds()
    {
        // Arrange - Ensure single-digit minutes/seconds are zero-padded
        var uptime = new TimeSpan(0, 1, 5, 3);

        // Act
        var result = StatusBarViewModel.FormatUptime(uptime);

        // Assert
        result.Should().Be("1:05:03");
    }
}
