using FluentAssertions;
using HexGame.Rendering;
using Xunit;

namespace HexMapTutorial.Tests.Rendering;

/// <summary>
/// Tests for PerformanceStatistics class.
/// Validates frame time tracking, statistics calculation, and edge cases.
/// </summary>
public class PerformanceStatisticsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultSize_CreatesEmptyStatistics()
    {
        var stats = new PerformanceStatistics();

        stats.FrameCount.Should().Be(0);
        stats.MaxHistorySize.Should().Be(200);
    }

    [Fact]
    public void Constructor_WithCustomSize_UsesCustomSize()
    {
        var stats = new PerformanceStatistics(100);

        stats.MaxHistorySize.Should().Be(100);
    }

    [Fact]
    public void Constructor_WithZeroSize_DefaultsTo200()
    {
        var stats = new PerformanceStatistics(0);

        stats.MaxHistorySize.Should().Be(200);
    }

    [Fact]
    public void Constructor_WithNegativeSize_DefaultsTo200()
    {
        var stats = new PerformanceStatistics(-5);

        stats.MaxHistorySize.Should().Be(200);
    }

    #endregion

    #region RecordFrame Tests

    [Fact]
    public void RecordFrame_SingleFrame_UpdatesFrameCount()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(16.67f);

        stats.FrameCount.Should().Be(1);
    }

    [Fact]
    public void RecordFrame_MultipleFrames_TracksAllFrames()
    {
        var stats = new PerformanceStatistics();

        for (int i = 0; i < 10; i++)
        {
            stats.RecordFrame(16.67f);
        }

        stats.FrameCount.Should().Be(10);
    }

    [Fact]
    public void RecordFrame_ExceedsMaxHistory_MaintainsMaxSize()
    {
        var stats = new PerformanceStatistics(50);

        for (int i = 0; i < 100; i++)
        {
            stats.RecordFrame(16.67f);
        }

        stats.FrameCount.Should().Be(50);
    }

    [Fact]
    public void RecordFrame_CircularBuffer_RemovesOldestFrames()
    {
        var stats = new PerformanceStatistics(5);

        // Record frames 1, 2, 3, 4, 5
        for (int i = 1; i <= 5; i++)
        {
            stats.RecordFrame(i);
        }

        // Record frame 100 - should push out frame 1
        stats.RecordFrame(100);

        var frameTimes = stats.GetFrameTimes();
        frameTimes.Should().NotContain(1.0f);
        frameTimes.Should().Contain(100.0f);
    }

    #endregion

    #region FPS Calculation Tests

    [Fact]
    public void Fps_At60FpsFrameTime_Returns60()
    {
        var stats = new PerformanceStatistics();

        // Record several frames at 16.67ms (60 FPS)
        for (int i = 0; i < 10; i++)
        {
            stats.RecordFrame(16.67f);
        }

        stats.Fps.Should().BeApproximately(60.0f, 1.0f);
    }

    [Fact]
    public void Fps_At30FpsFrameTime_Returns30()
    {
        var stats = new PerformanceStatistics();

        // Record several frames at 33.33ms (30 FPS)
        for (int i = 0; i < 10; i++)
        {
            stats.RecordFrame(33.33f);
        }

        stats.Fps.Should().BeApproximately(30.0f, 1.0f);
    }

    [Fact]
    public void Fps_WithNoFrames_ReturnsDefault60()
    {
        var stats = new PerformanceStatistics();

        stats.Fps.Should().Be(60.0f);
    }

    [Fact]
    public void Fps_WithZeroFrameTime_ReturnsDefault60()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(0.0f);

        // With zero frame time, division would fail, so should return default
        stats.Fps.Should().Be(60.0f);
    }

    #endregion

    #region Average Frame Time Tests

    [Fact]
    public void AverageFrameTimeMs_SingleFrame_ReturnsThatFrameTime()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(20.0f);

        stats.AverageFrameTimeMs.Should().Be(20.0f);
    }

    [Fact]
    public void AverageFrameTimeMs_MultipleFrames_ReturnsCorrectAverage()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(10.0f);
        stats.RecordFrame(20.0f);
        stats.RecordFrame(30.0f);

        stats.AverageFrameTimeMs.Should().Be(20.0f);
    }

    [Fact]
    public void AverageFrameTimeMs_WithNoFrames_ReturnsTarget()
    {
        var stats = new PerformanceStatistics();

        stats.AverageFrameTimeMs.Should().Be(RenderingConfig.TargetFrameTimeMs);
    }

    [Fact]
    public void AverageFrameTimeMs_RunningSum_RemainsAccurate()
    {
        var stats = new PerformanceStatistics(5);

        // Fill buffer with 10ms frames
        for (int i = 0; i < 5; i++)
        {
            stats.RecordFrame(10.0f);
        }
        stats.AverageFrameTimeMs.Should().Be(10.0f);

        // Replace with 20ms frames
        for (int i = 0; i < 5; i++)
        {
            stats.RecordFrame(20.0f);
        }
        stats.AverageFrameTimeMs.Should().Be(20.0f);
    }

    #endregion

    #region Min/Max Frame Time Tests

    [Fact]
    public void MaxFrameTimeMs_ReturnsLargestFrame()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(10.0f);
        stats.RecordFrame(50.0f);
        stats.RecordFrame(20.0f);

        stats.MaxFrameTimeMs.Should().Be(50.0f);
    }

    [Fact]
    public void MinFrameTimeMs_ReturnsSmallestFrame()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(30.0f);
        stats.RecordFrame(10.0f);
        stats.RecordFrame(20.0f);

        stats.MinFrameTimeMs.Should().Be(10.0f);
    }

    [Fact]
    public void MaxFrameTimeMs_WithNoFrames_ReturnsZero()
    {
        var stats = new PerformanceStatistics();

        stats.MaxFrameTimeMs.Should().Be(0.0f);
    }

    [Fact]
    public void MinFrameTimeMs_WithNoFrames_Returns1000()
    {
        var stats = new PerformanceStatistics();

        stats.MinFrameTimeMs.Should().Be(1000.0f);
    }

    #endregion

    #region One Percent Low Tests

    [Fact]
    public void OnePercentLowFps_WithFewFrames_EqualsFps()
    {
        var stats = new PerformanceStatistics();

        // Less than 10 frames
        for (int i = 0; i < 5; i++)
        {
            stats.RecordFrame(16.67f);
        }

        stats.OnePercentLowFps.Should().BeApproximately(stats.Fps, 1.0f);
    }

    [Fact]
    public void OnePercentLowFps_WithEnoughFrames_ReturnsWorstPercentile()
    {
        var stats = new PerformanceStatistics();

        // Record 90 fast frames (16.67ms = 60 FPS)
        for (int i = 0; i < 90; i++)
        {
            stats.RecordFrame(16.67f);
        }

        // Record 10 slow frames (100ms = 10 FPS) - these are the worst 10%
        for (int i = 0; i < 10; i++)
        {
            stats.RecordFrame(100.0f);
        }

        // 1% low is calculated as the frame at index (count * 0.01) in descending order
        // With 100 frames, this is index 1, which gives us the 99th percentile threshold
        // Since we have 10 slow frames, the 1% low should be based on a slow frame
        stats.OnePercentLowFps.Should().BeApproximately(10.0f, 1.0f);
    }

    [Fact]
    public void OnePercentLowFps_AllSameFrameTime_EqualsNormalFps()
    {
        var stats = new PerformanceStatistics();

        for (int i = 0; i < 100; i++)
        {
            stats.RecordFrame(16.67f);
        }

        stats.OnePercentLowFps.Should().BeApproximately(stats.Fps, 1.0f);
    }

    #endregion

    #region GetFrameTimes Tests

    [Fact]
    public void GetFrameTimes_ReturnsAllRecordedFrames()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(10.0f);
        stats.RecordFrame(20.0f);
        stats.RecordFrame(30.0f);

        var frameTimes = stats.GetFrameTimes();

        frameTimes.Should().HaveCount(3);
        frameTimes.Should().Contain(10.0f);
        frameTimes.Should().Contain(20.0f);
        frameTimes.Should().Contain(30.0f);
    }

    [Fact]
    public void GetFrameTimes_ReturnsCopy_NotOriginal()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(10.0f);
        var frameTimes1 = stats.GetFrameTimes();

        stats.RecordFrame(20.0f);
        var frameTimes2 = stats.GetFrameTimes();

        frameTimes1.Should().HaveCount(1);
        frameTimes2.Should().HaveCount(2);
    }

    [Fact]
    public void GetFrameTimes_EmptyStats_ReturnsEmptyArray()
    {
        var stats = new PerformanceStatistics();

        var frameTimes = stats.GetFrameTimes();

        frameTimes.Should().BeEmpty();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ResetsFrameCount()
    {
        var stats = new PerformanceStatistics();

        for (int i = 0; i < 50; i++)
        {
            stats.RecordFrame(16.67f);
        }

        stats.Clear();

        stats.FrameCount.Should().Be(0);
    }

    [Fact]
    public void Clear_ResetsStatistics()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(100.0f); // Slow frame

        stats.Clear();

        stats.Fps.Should().Be(60.0f);
        stats.AverageFrameTimeMs.Should().Be(RenderingConfig.TargetFrameTimeMs);
        stats.MaxFrameTimeMs.Should().Be(0.0f);
    }

    [Fact]
    public void Clear_AllowsReuse()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(100.0f);
        stats.Clear();

        stats.RecordFrame(16.67f);
        stats.RecordFrame(16.67f);

        stats.FrameCount.Should().Be(2);
        stats.Fps.Should().BeApproximately(60.0f, 1.0f);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RecordFrame_VerySmallFrameTime_HandledCorrectly()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(0.001f); // 1 microsecond

        stats.FrameCount.Should().Be(1);
        stats.Fps.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordFrame_VeryLargeFrameTime_HandledCorrectly()
    {
        var stats = new PerformanceStatistics();

        stats.RecordFrame(10000.0f); // 10 seconds

        stats.FrameCount.Should().Be(1);
        stats.Fps.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Statistics_WithMixedFrameTimes_CalculatesCorrectly()
    {
        var stats = new PerformanceStatistics();

        // Simulate variable frame rate
        stats.RecordFrame(16.0f);
        stats.RecordFrame(17.0f);
        stats.RecordFrame(15.0f);
        stats.RecordFrame(18.0f);
        stats.RecordFrame(14.0f);

        // Average should be 16ms
        stats.AverageFrameTimeMs.Should().Be(16.0f);
        stats.MaxFrameTimeMs.Should().Be(18.0f);
        stats.MinFrameTimeMs.Should().Be(14.0f);
    }

    #endregion
}
