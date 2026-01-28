namespace HexGame.Rendering;

/// <summary>
/// Calculates frame time statistics for performance monitoring.
/// Uses a circular buffer (Queue) for O(1) frame recording.
/// </summary>
public class PerformanceStatistics
{
    private readonly Queue<float> _frameTimes;
    private readonly int _maxHistorySize;

    // Cached statistics (recalculated on each frame)
    private float _fps = 60.0f;
    private float _avgFrameTime = RenderingConfig.TargetFrameTimeMs;
    private float _maxFrameTime = 0.0f;
    private float _minFrameTime = 1000.0f;
    private float _onePercentLow = 60.0f;

    // Running sum for O(1) average calculation
    private float _runningSum = 0.0f;

    /// <summary>
    /// Creates a new performance statistics tracker.
    /// </summary>
    /// <param name="maxHistorySize">Maximum number of frames to track.</param>
    public PerformanceStatistics(int maxHistorySize = 200)
    {
        _maxHistorySize = maxHistorySize > 0 ? maxHistorySize : 200;
        _frameTimes = new Queue<float>(_maxHistorySize);
    }

    /// <summary>
    /// Records a frame time and updates statistics.
    /// </summary>
    /// <param name="deltaMs">Frame time in milliseconds.</param>
    public void RecordFrame(float deltaMs)
    {
        // Enforce capacity using Queue (O(1) dequeue vs O(n) RemoveAt(0))
        if (_frameTimes.Count >= _maxHistorySize)
        {
            float removed = _frameTimes.Dequeue();
            _runningSum -= removed;
        }

        _frameTimes.Enqueue(deltaMs);
        _runningSum += deltaMs;

        RecalculateStatistics();
    }

    /// <summary>
    /// Recalculates all statistics from current frame data.
    /// </summary>
    private void RecalculateStatistics()
    {
        int count = _frameTimes.Count;
        if (count == 0)
        {
            _fps = 60.0f;
            _avgFrameTime = RenderingConfig.TargetFrameTimeMs;
            _maxFrameTime = 0.0f;
            _minFrameTime = 1000.0f;
            _onePercentLow = 60.0f;
            return;
        }

        // Calculate average from running sum (O(1))
        _avgFrameTime = _runningSum / count;
        _fps = _avgFrameTime > 0 ? 1000.0f / _avgFrameTime : 60.0f;

        // Find min/max (must iterate, but could be optimized with tracked values)
        _maxFrameTime = 0.0f;
        _minFrameTime = 1000.0f;
        foreach (float t in _frameTimes)
        {
            if (t > _maxFrameTime) _maxFrameTime = t;
            if (t < _minFrameTime) _minFrameTime = t;
        }

        // Calculate 1% low (99th percentile worst frames)
        // Only calculate when we have enough samples
        if (count >= 10)
        {
            // Use a more efficient partial sort for large datasets
            // For 200 frames, full sort is acceptable
            var sortedTimes = _frameTimes.OrderByDescending(t => t).ToArray();
            int onePercentIndex = Math.Max(0, (int)(count * 0.01f));
            float worstFrameTime = sortedTimes[onePercentIndex];
            _onePercentLow = worstFrameTime > 0 ? 1000.0f / worstFrameTime : 60.0f;
        }
        else
        {
            _onePercentLow = _fps;
        }
    }

    /// <summary>
    /// Gets the current FPS.
    /// </summary>
    public float Fps => _fps;

    /// <summary>
    /// Gets the average frame time in milliseconds.
    /// </summary>
    public float AverageFrameTimeMs => _avgFrameTime;

    /// <summary>
    /// Gets the maximum frame time in the history.
    /// </summary>
    public float MaxFrameTimeMs => _maxFrameTime;

    /// <summary>
    /// Gets the minimum frame time in the history.
    /// </summary>
    public float MinFrameTimeMs => _minFrameTime;

    /// <summary>
    /// Gets the 1% low FPS (99th percentile worst frames).
    /// </summary>
    public float OnePercentLowFps => _onePercentLow;

    /// <summary>
    /// Gets the number of frames in the history.
    /// </summary>
    public int FrameCount => _frameTimes.Count;

    /// <summary>
    /// Gets the maximum history size.
    /// </summary>
    public int MaxHistorySize => _maxHistorySize;

    /// <summary>
    /// Gets a copy of the frame times for graph rendering.
    /// </summary>
    public float[] GetFrameTimes() => _frameTimes.ToArray();

    /// <summary>
    /// Clears all frame history and resets statistics.
    /// </summary>
    public void Clear()
    {
        _frameTimes.Clear();
        _runningSum = 0.0f;
        _fps = 60.0f;
        _avgFrameTime = RenderingConfig.TargetFrameTimeMs;
        _maxFrameTime = 0.0f;
        _minFrameTime = 1000.0f;
        _onePercentLow = 60.0f;
    }
}
