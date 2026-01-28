using Godot;
using HexGame.Rendering;

namespace HexGame.UI;

/// <summary>
/// Performance monitoring panel displayed in the lower-left corner.
/// Shows a frame time graph and statistics (FPS, avg, 1% low, max).
/// </summary>
public partial class PerformanceMonitor : Control
{
    private readonly PerformanceStatistics _stats = new(RenderingConfig.FrameHistorySize);

    private Panel? _backgroundPanel;
    private Label? _statsLabel;
    private Control? _graphControl;

    // Colors for the graph
    private static readonly Color GoodColor = new(0.2f, 0.8f, 0.2f);      // Green - 60+ FPS
    private static readonly Color WarningColor = new(0.8f, 0.8f, 0.2f);   // Yellow - 30-60 FPS
    private static readonly Color BadColor = new(0.8f, 0.2f, 0.2f);       // Red - <30 FPS
    private static readonly Color BackgroundColor = new(0, 0, 0, 0.7f);

    public override void _Ready()
    {
        SetupPanel();
    }

    public override void _Process(double delta)
    {
        _stats.RecordFrame((float)(delta * 1000.0)); // Convert to milliseconds
        UpdateStatsLabel();
        _graphControl?.QueueRedraw();
    }

    private void SetupPanel()
    {
        // Position in lower-left corner
        AnchorLeft = 0;
        AnchorTop = 1;
        AnchorRight = 0;
        AnchorBottom = 1;

        int width = RenderingConfig.PerformanceGraphWidth;
        int height = RenderingConfig.PerformanceGraphHeight;
        int margin = RenderingConfig.PanelMargin;

        // Total height includes graph + stats label
        int totalHeight = height + 40; // 40 for stats label

        OffsetLeft = margin;
        OffsetTop = -totalHeight - margin;
        OffsetRight = width + margin;
        OffsetBottom = -margin;

        // Background panel
        _backgroundPanel = new Panel();
        _backgroundPanel.SetAnchorsPreset(LayoutPreset.FullRect);

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = BackgroundColor;
        styleBox.SetCornerRadiusAll(4);
        _backgroundPanel.AddThemeStyleboxOverride("panel", styleBox);
        AddChild(_backgroundPanel);

        // Container for layout
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 4);
        AddChild(vbox);

        // Graph control (custom drawing)
        _graphControl = new Control();
        _graphControl.CustomMinimumSize = new Vector2(width, height);
        _graphControl.Draw += OnGraphDraw;
        vbox.AddChild(_graphControl);

        // Stats label
        _statsLabel = new Label();
        _statsLabel.HorizontalAlignment = HorizontalAlignment.Left;
        _statsLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_statsLabel);

        UpdateStatsLabel();
    }

    private void UpdateStatsLabel()
    {
        if (_statsLabel == null) return;

        _statsLabel.Text = $"FPS: {(int)_stats.Fps} | Avg: {_stats.AverageFrameTimeMs:F1}ms | 1%Low: {(int)_stats.OnePercentLowFps} | Max: {_stats.MaxFrameTimeMs:F1}ms";
    }

    private void OnGraphDraw()
    {
        if (_graphControl == null || _stats.FrameCount == 0)
            return;

        var size = _graphControl.Size;
        var frameTimes = _stats.GetFrameTimes();
        float barWidth = size.X / RenderingConfig.FrameHistorySize;
        float maxMs = 50.0f; // Max frame time for graph scale (50ms)

        // Draw each frame as a vertical bar
        for (int i = 0; i < frameTimes.Length; i++)
        {
            float frameTime = frameTimes[i];
            float normalizedHeight = Mathf.Clamp(frameTime / maxMs, 0, 1);
            float barHeight = normalizedHeight * size.Y;

            // Color based on performance
            Color barColor;
            if (frameTime <= RenderingConfig.TargetFrameTimeMs)
                barColor = GoodColor;
            else if (frameTime <= RenderingConfig.WarningFrameTimeMs)
                barColor = WarningColor;
            else
                barColor = BadColor;

            // Draw from bottom up
            float x = i * barWidth;
            float y = size.Y - barHeight;

            _graphControl.DrawRect(new Rect2(x, y, barWidth, barHeight), barColor);
        }

        // Draw threshold lines
        float targetY = size.Y - (RenderingConfig.TargetFrameTimeMs / maxMs * size.Y);
        float warningY = size.Y - (RenderingConfig.WarningFrameTimeMs / maxMs * size.Y);

        _graphControl.DrawLine(new Vector2(0, targetY), new Vector2(size.X, targetY), GoodColor.Lightened(0.3f), 1.0f);
        _graphControl.DrawLine(new Vector2(0, warningY), new Vector2(size.X, warningY), WarningColor.Lightened(0.3f), 1.0f);
    }

    /// <summary>
    /// Gets the current FPS.
    /// </summary>
    public float CurrentFps => _stats.Fps;

    /// <summary>
    /// Gets the average frame time in milliseconds.
    /// </summary>
    public float AverageFrameTimeMs => _stats.AverageFrameTimeMs;

    /// <summary>
    /// Gets the 1% low FPS (99th percentile worst frames).
    /// </summary>
    public float OnePercentLowFps => _stats.OnePercentLowFps;

    /// <summary>
    /// Gets the maximum frame time recorded.
    /// </summary>
    public float MaxFrameTimeMs => _stats.MaxFrameTimeMs;

    /// <summary>
    /// Gets the underlying statistics tracker for testing.
    /// </summary>
    public PerformanceStatistics Statistics => _stats;
}
