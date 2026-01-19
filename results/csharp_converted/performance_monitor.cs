using Godot;
using Godot.Collections;


//# Real-time performance monitoring with FPS graph

//# Matches web/src/utils/PerformanceMonitor.ts
// Graph dimensions
[GlobalClass]
public partial class PerformanceMonitor : Godot.Control
{
	public const int GRAPH_WIDTH = 200;
	public const int GRAPH_HEIGHT = 60;
	public const int HISTORY_SIZE = 200;


	// Thresholds
	public const double TARGET_FRAME_TIME = 16.67;
	// 60 fps
	public const double WARNING_FRAME_TIME = 33.33;

	// 30 fps
	// Performance data
	public Array<float> FrameTimes = new Array{};
	public double Fps = 60.0;
	public double AvgFrameTime = 16.67;
	public double MaxFrameTime = 0.0;
	public double MinFrameTime = 1000.0;
	public double OnePercentLow = 60.0;

	// 1% low FPS
	// UI elements
	public Godot.ColorRect GraphRect;
	public Godot.TextureRect GraphTexture;
	public Godot.Image Image;
	public Godot.Label StatsLabel;
	public bool VisibleGraph = true;


	public override void _Ready()
	{
		_SetupUi();

		// Initialize frame time history
		foreach(int i in GD.Range(HISTORY_SIZE))
		{
			FrameTimes.Append(16.67);
		}
	}


	protected void _SetupUi()
	{

		// Container at bottom-left
		AnchorLeft = 0;
		AnchorTop = 1;
		AnchorRight = 0;
		AnchorBottom = 1;
		OffsetLeft = 10;
		OffsetTop =  - 80;
		OffsetRight = GRAPH_WIDTH + 10;
		OffsetBottom =  - 10;


		// Background
		GraphRect = ColorRect.New();
		GraphRect.Color = new Color(0, 0, 0, 0.7);
		GraphRect.Size = new Vector2(GRAPH_WIDTH, GRAPH_HEIGHT + 20);
		AddChild(GraphRect);


		// Graph image
		Image = Image.Create(GRAPH_WIDTH, GRAPH_HEIGHT, false, Image.Format.FormatRgba8);
		Image.Fill(new Color(0, 0, 0, 0));

		var texture = ImageTexture.CreateFromImage(Image);
		GraphTexture = TextureRect.New();
		GraphTexture.Texture = texture;
		GraphTexture.Position = new Vector2(0, 20);
		GraphTexture.Size = new Vector2(GRAPH_WIDTH, GRAPH_HEIGHT);
		AddChild(GraphTexture);


		// Stats label
		StatsLabel = Label.New();
		StatsLabel.Position = new Vector2(4, 2);
		StatsLabel.AddThemeFontSizeOverride("font_size", 11);
		StatsLabel.AddThemeColorOverride("font_color", Color.White);
		AddChild(StatsLabel);
	}


	public override void _Process(double delta)
	{
		RecordFrame(delta * 1000.0);
		// Convert to ms
		_UpdateDisplay();
	}


	public void RecordFrame(double delta_ms)
	{

		// Add new frame time
		FrameTimes.Append(delta_ms);
		if(FrameTimes.Size() > HISTORY_SIZE)
		{
			FrameTimes.PopFront();
		}


		// Calculate statistics
		var sum = 0.0;
		MaxFrameTime = 0.0;
		MinFrameTime = 1000.0;

		foreach(float t in FrameTimes)
		{
			sum += t;
			MaxFrameTime = Mathf.Max(MaxFrameTime, t);
			MinFrameTime = Mathf.Min(MinFrameTime, t);
		}

		AvgFrameTime = sum / FrameTimes.Size();
		Fps = ( AvgFrameTime > 0 ? 1000.0 / AvgFrameTime : 60.0 );


		// Calculate 1% low (worst 1% of frames)
		var sorted_times = FrameTimes.Duplicate();
		sorted_times.Sort();
		var one_percent_index = Int(sorted_times.Size() * 0.99);
		var worst_one_percent = sorted_times[one_percent_index];
		OnePercentLow = ( worst_one_percent > 0 ? 1000.0 / worst_one_percent : 60.0 );
	}


	protected void _UpdateDisplay()
	{

		// Update stats label
		StatsLabel.Text = "FPS: %d | Avg: %.1fms" % new Array{Int(Fps), AvgFrameTime, };
		StatsLabel.Text += "\n1%% Low: %d | Max: %.1fms" % new Array{Int(OnePercentLow), MaxFrameTime, };

		if(!VisibleGraph)
		{
			return ;
		}


		// Clear image
		Image.Fill(new Color(0, 0, 0, 0));


		// Draw threshold lines
		var target_y = Int((1.0 - TARGET_FRAME_TIME / 50.0) * GRAPH_HEIGHT);
		var warning_y = Int((1.0 - WARNING_FRAME_TIME / 50.0) * GRAPH_HEIGHT);


		// Draw dashed lines for thresholds
		foreach(int x in GD.Range(0, GRAPH_WIDTH, 4))
		{
			if(target_y >= 0 && target_y < GRAPH_HEIGHT)
			{
				Image.SetPixel(x, target_y, new Color(0, 0.8, 0, 0.5));
			}
			// Green 60fps line
			if(warning_y >= 0 && warning_y < GRAPH_HEIGHT)
			{
				Image.SetPixel(x, warning_y, new Color(0.8, 0.8, 0, 0.5));

				// Yellow 30fps line

			}
		}// Draw frame time bars
		var bar_width = 1;
		foreach(int i in GD.Range(FrameTimes.Size()))
		{
			var t = FrameTimes[i];
			var x = i * GRAPH_WIDTH / HISTORY_SIZE;
			var height = Int(Mathf.Min(t / 50.0, 1.0) * GRAPH_HEIGHT);
			var y_start = GRAPH_HEIGHT - height;


			// Color based on frame time
			var color;
			if(t <= TARGET_FRAME_TIME)
			{
				color = new Color(0, 0.8, 0, 0.9);
			}
			// Green - good
			else if(t <= WARNING_FRAME_TIME)
			{
				color = new Color(0.8, 0.8, 0, 0.9);
			}
			// Yellow - warning
			else
			{
				color = new Color(0.8, 0, 0, 0.9);

				// Red - bad

			}// Draw vertical bar
			foreach(int y in GD.Range(y_start, GRAPH_HEIGHT))
			{
				if(x >= 0 && x < GRAPH_WIDTH && y >= 0 && y < GRAPH_HEIGHT)
				{
					Image.SetPixel(x, y, color);
				}
			}
		}


		// Update texture
		var texture = ImageTexture.CreateFromImage(Image);
		GraphTexture.Texture = texture;
	}


	public void ToggleGraph()
	{
		VisibleGraph = !VisibleGraph;
		GraphTexture.Visible = VisibleGraph;
	}


	public Dictionary GetStats()
	{
		return new Dictionary{
					{"fps", Fps},
					{"avg_frame_time", AvgFrameTime},
					{"max_frame_time", MaxFrameTime},
					{"min_frame_time", MinFrameTime},
					{"one_percent_low", OnePercentLow},
					};
	}


}