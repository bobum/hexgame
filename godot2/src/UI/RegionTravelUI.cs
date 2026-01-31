using Godot;
using System;
using System.Threading.Tasks;
using HexGame.Region;

namespace HexGame.UI;

/// <summary>
/// Travel transition screen shown while loading a new region.
/// Displays an animated ocean scene with progress feedback.
/// </summary>
public partial class RegionTravelUI : Control
{
    #region Signals

    /// <summary>
    /// Emitted when the player cancels the voyage.
    /// </summary>
    [Signal]
    public delegate void TravelCancelledEventHandler();

    /// <summary>
    /// Emitted when travel completes successfully.
    /// </summary>
    [Signal]
    public delegate void TravelCompletedEventHandler();

    #endregion

    #region Configuration

    /// <summary>
    /// Minimum time to show the travel screen (immersion).
    /// </summary>
    private const float MinTravelTimeSeconds = 2.0f;

    /// <summary>
    /// Colors for the ocean gradient.
    /// </summary>
    private static readonly Color DeepOceanColor = new(0.05f, 0.12f, 0.25f);
    private static readonly Color SurfaceOceanColor = new(0.1f, 0.25f, 0.45f);
    private static readonly Color WaveHighlightColor = new(0.2f, 0.4f, 0.6f);

    #endregion

    #region UI Elements

    private ColorRect? _oceanBackground;
    private Control? _waveLayer1;
    private Control? _waveLayer2;
    private Control? _shipSprite;
    private Label? _flavorTextLabel;
    private Label? _destinationLabel;
    private ProgressBar? _progressBar;
    private Label? _progressLabel;
    private Button? _cancelButton;
    private Panel? _textPanel;

    #endregion

    #region State

    private float _elapsedTime;
    private float _targetProgress;
    private float _displayedProgress;
    private bool _isTraveling;
    private bool _canCancel;
    private string _currentStage = "";
    private RegionMapEntry? _fromRegion;
    private RegionMapEntry? _toRegion;

    // Ship bobbing animation
    private float _shipBobPhase;
    private const float ShipBobSpeed = 2.0f;
    private const float ShipBobAmplitude = 8.0f;
    private Vector2 _shipBasePosition;

    // Wave animation
    private float _wavePhase;
    private const float WaveSpeed = 0.5f;

    #endregion

    #region Flavor Text

    private static readonly string[] FlavorTexts = new[]
    {
        "The salt air fills your lungs as you set sail...",
        "Waves lap against the hull as the crew works...",
        "Seabirds circle overhead, following your course...",
        "The compass needle holds steady to the horizon...",
        "Distant clouds gather on the horizon ahead...",
        "The crew hoists the mainsail, catching the wind...",
        "Stars begin to appear in the darkening sky...",
        "The ship creaks and groans in the heavy swells...",
        "A pod of dolphins races alongside the bow...",
        "The navigator marks your progress on the chart..."
    };

    #endregion

    public override void _Ready()
    {
        Visible = false;
        BuildUI();
    }

    public override void _Process(double delta)
    {
        if (!_isTraveling) return;

        _elapsedTime += (float)delta;

        // Smooth progress bar animation
        _displayedProgress = Mathf.Lerp(_displayedProgress, _targetProgress, (float)delta * 3f);
        if (_progressBar != null)
        {
            _progressBar.Value = _displayedProgress * 100;
        }

        // Animate waves
        _wavePhase += (float)delta * WaveSpeed;
        AnimateWaves();

        // Animate ship bobbing
        _shipBobPhase += (float)delta * ShipBobSpeed;
        AnimateShip();
    }

    /// <summary>
    /// Shows the travel screen and begins the voyage animation.
    /// </summary>
    public async Task ShowTravelAsync(
        RegionMapEntry fromRegion,
        RegionMapEntry toRegion,
        Func<Task<bool>> loadRegionTask)
    {
        _fromRegion = fromRegion;
        _toRegion = toRegion;
        _isTraveling = true;
        _canCancel = true;
        _elapsedTime = 0f;
        _targetProgress = 0f;
        _displayedProgress = 0f;
        _currentStage = "Preparing";

        // Set destination text
        if (_destinationLabel != null)
        {
            _destinationLabel.Text = $"Destination: {toRegion.Name}";
        }

        // Pick random flavor text
        if (_flavorTextLabel != null)
        {
            var rng = new Random();
            _flavorTextLabel.Text = FlavorTexts[rng.Next(FlavorTexts.Length)];
        }

        UpdateProgress("Setting sail...", 0f);
        Show();

        // Start the loading task
        var loadTask = loadRegionTask();

        // Wait for minimum travel time OR load completion
        var minTimeTask = WaitMinimumTime();

        // Wait for both minimum time and loading
        await Task.WhenAll(minTimeTask, loadTask);

        var loadSuccess = await loadTask;

        if (!_isTraveling)
        {
            // Was cancelled
            return;
        }

        if (loadSuccess)
        {
            UpdateProgress("Arriving at destination...", 1f);
            await ToSignal(GetTree().CreateTimer(0.5f), Godot.Timer.SignalName.Timeout);

            _isTraveling = false;
            Hide();
            EmitSignal(SignalName.TravelCompleted);
        }
        else
        {
            UpdateProgress("Navigation failed!", _displayedProgress);
            _canCancel = true;
            _cancelButton!.Text = "Return";
        }
    }

    /// <summary>
    /// Updates the progress display during loading.
    /// </summary>
    public void UpdateProgress(string stage, float progress)
    {
        _currentStage = stage;
        _targetProgress = Mathf.Clamp(progress, 0f, 1f);

        if (_progressLabel != null)
        {
            _progressLabel.Text = stage;
        }
    }

    private async Task WaitMinimumTime()
    {
        while (_elapsedTime < MinTravelTimeSeconds && _isTraveling)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void BuildUI()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        // Ocean background gradient
        _oceanBackground = new ColorRect();
        _oceanBackground.SetAnchorsPreset(LayoutPreset.FullRect);
        _oceanBackground.Color = DeepOceanColor;
        AddChild(_oceanBackground);

        // Wave layers (for animation)
        _waveLayer1 = CreateWaveLayer(0.6f);
        AddChild(_waveLayer1);

        _waveLayer2 = CreateWaveLayer(0.8f);
        AddChild(_waveLayer2);

        // Ship in center
        _shipSprite = CreateShipSprite();
        _shipBasePosition = GetViewportRect().Size / 2;
        _shipSprite.Position = _shipBasePosition;
        AddChild(_shipSprite);

        // Text panel at bottom
        _textPanel = new Panel();
        _textPanel.AnchorLeft = 0.1f;
        _textPanel.AnchorRight = 0.9f;
        _textPanel.AnchorTop = 0.7f;
        _textPanel.AnchorBottom = 0.95f;
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.7f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 15,
            ContentMarginBottom = 15
        };
        _textPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_textPanel);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 10);
        _textPanel.AddChild(vbox);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 15);
        margin.AddThemeConstantOverride("margin_bottom", 15);
        _textPanel.AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 12);
        margin.AddChild(content);

        // Destination
        _destinationLabel = new Label
        {
            Text = "Destination: Unknown",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _destinationLabel.AddThemeFontSizeOverride("font_size", 20);
        _destinationLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        content.AddChild(_destinationLabel);

        // Flavor text
        _flavorTextLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        _flavorTextLabel.AddThemeFontSizeOverride("font_size", 14);
        _flavorTextLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        content.AddChild(_flavorTextLabel);

        // Progress bar
        _progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 25)
        };
        content.AddChild(_progressBar);

        // Progress label
        _progressLabel = new Label
        {
            Text = "Preparing...",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _progressLabel.AddThemeFontSizeOverride("font_size", 12);
        content.AddChild(_progressLabel);

        // Cancel button
        var buttonContainer = new CenterContainer();
        content.AddChild(buttonContainer);

        _cancelButton = new Button
        {
            Text = "Cancel Voyage",
            CustomMinimumSize = new Vector2(150, 35)
        };
        _cancelButton.Pressed += OnCancelPressed;
        buttonContainer.AddChild(_cancelButton);
    }

    private Control CreateWaveLayer(float heightFraction)
    {
        var wave = new ColorRect();
        wave.SetAnchorsPreset(LayoutPreset.BottomWide);
        wave.OffsetTop = -(GetViewportRect().Size.Y * (1 - heightFraction));
        wave.Color = new Color(
            SurfaceOceanColor.R + (heightFraction - 0.5f) * 0.1f,
            SurfaceOceanColor.G + (heightFraction - 0.5f) * 0.15f,
            SurfaceOceanColor.B + (heightFraction - 0.5f) * 0.1f,
            0.4f + heightFraction * 0.3f
        );
        return wave;
    }

    private Control CreateShipSprite()
    {
        // Simple ship representation using basic shapes
        var ship = new Control();
        ship.CustomMinimumSize = new Vector2(80, 60);

        // Hull
        var hull = new ColorRect
        {
            Color = new Color(0.4f, 0.25f, 0.15f),
            Size = new Vector2(80, 30),
            Position = new Vector2(-40, 0)
        };
        ship.AddChild(hull);

        // Sail
        var sail = new ColorRect
        {
            Color = new Color(0.95f, 0.9f, 0.85f),
            Size = new Vector2(40, 50),
            Position = new Vector2(-20, -55)
        };
        ship.AddChild(sail);

        // Mast
        var mast = new ColorRect
        {
            Color = new Color(0.35f, 0.2f, 0.1f),
            Size = new Vector2(4, 60),
            Position = new Vector2(-2, -55)
        };
        ship.AddChild(mast);

        return ship;
    }

    private void AnimateWaves()
    {
        // Subtle vertical movement for wave layers
        if (_waveLayer1 != null)
        {
            var offset = Mathf.Sin(_wavePhase * Mathf.Pi * 2) * 5f;
            _waveLayer1.Position = new Vector2(0, offset);
        }

        if (_waveLayer2 != null)
        {
            var offset = Mathf.Sin(_wavePhase * Mathf.Pi * 2 + 1f) * 3f;
            _waveLayer2.Position = new Vector2(0, offset);
        }
    }

    private void AnimateShip()
    {
        if (_shipSprite == null) return;

        // Bob up and down
        var bobOffset = Mathf.Sin(_shipBobPhase * Mathf.Pi) * ShipBobAmplitude;

        // Slight rotation for rocking
        var rockAngle = Mathf.Sin(_shipBobPhase * 0.7f * Mathf.Pi) * 0.05f;

        _shipSprite.Position = new Vector2(
            _shipBasePosition.X,
            _shipBasePosition.Y * 0.5f + bobOffset
        );
        _shipSprite.Rotation = rockAngle;
    }

    private void OnCancelPressed()
    {
        if (!_canCancel) return;

        _isTraveling = false;
        Hide();
        EmitSignal(SignalName.TravelCancelled);
    }
}
