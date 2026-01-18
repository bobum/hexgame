using Godot;
using Godot.Collections;


//# Orbital camera with mouse and keyboard controls

//# Matches web/src/camera/MapCamera.ts
// Camera configuration
[GlobalClass]
public partial class MapCamera : Godot.Camera3D
{
	[Export] public double MinZoom = 5.0;
	[Export] public double MaxZoom = 80.0;
	[Export] public double MinPitch = 20.0;
	[Export] public double MaxPitch = 85.0;
	[Export] public double PanSpeed = 0.5;
	[Export] public double RotateSpeed = 0.3;
	[Export] public double ZoomSpeed = 0.1;
	[Export] public double KeyboardPanSpeed = 20.0;
	[Export] public double KeyboardRotateSpeed = 60.0;


	// Orbital camera state
	public Vector3 Target = Vector3.Zero;
	public double Distance = 30.0;
	public double Pitch = 45.0;
	// Degrees
	public double Yaw = 0.0;

	// Degrees
	// Target values for smooth interpolation
	public double TargetDistance = 30.0;
	public double TargetPitch = 45.0;
	public double TargetYaw = 0.0;
	public Vector3 TargetPosition = Vector3.Zero;


	// Input state
	public bool IsPanning = false;
	public bool IsRotating = false;
	public Vector2 LastMousePos = Vector2.Zero;


	// Smoothing factor
	public double Smoothing = 10.0;


	public override void _Ready()
	{
		TargetPosition = Target;
		Far = 200.0;
		_UpdateCameraPosition();
		_UpdateNearPlane();
	}


	public override void _Process(double delta)
	{
		_HandleKeyboardInput(delta);
		_ApplySmoothing(delta);
		_UpdateCameraPosition();
		_UpdateNearPlane();
	}


	protected void _UpdateNearPlane()
	{

		// Dynamic near plane: closer when zoomed in, farther when zoomed out
		// This balances depth precision vs not clipping close terrain
		// At min_zoom (5), near = 0.5; at max_zoom (80), near = 4.0
		var t = (Distance - MinZoom) / (MaxZoom - MinZoom);
		Near = Mathf.Lerp(0.5, 4.0, t);
	}


	public override void _UnhandledInput(Godot.InputEvent event)
	{

		// Mouse wheel zoom
		if(event is Godot.InputEventMouseButton)
		{
			var mb = event;
			if(mb.Pressed)
			{
				if(mb.ButtonIndex == MOUSE_BUTTON_WHEEL_UP)
				{
					TargetDistance *= 0.9;
					TargetDistance = Mathf.Clamp(TargetDistance, MinZoom, MaxZoom);
				}
				else if(mb.ButtonIndex == MOUSE_BUTTON_WHEEL_DOWN)
				{
					TargetDistance *= 1.1;
					TargetDistance = Mathf.Clamp(TargetDistance, MinZoom, MaxZoom);
				}

				// Middle mouse button - start panning
				else if(mb.ButtonIndex == MOUSE_BUTTON_MIDDLE)
				{
					IsPanning = true;
					LastMousePos = mb.Position;
				}

				// Right mouse button - start rotating
				else if(mb.ButtonIndex == MOUSE_BUTTON_RIGHT)
				{
					IsRotating = true;
					LastMousePos = mb.Position;
				}
			}
			else
			{

				// Button released
				if(mb.ButtonIndex == MOUSE_BUTTON_MIDDLE)
				{
					IsPanning = false;
				}
				else if(mb.ButtonIndex == MOUSE_BUTTON_RIGHT)
				{
					IsRotating = false;
				}
			}
		}


		// Mouse motion for pan/rotate
		if(event is Godot.InputEventMouseMotion)
		{
			var mm = event;
			if(IsPanning)
			{
				_HandlePan(mm.Relative);
			}
			if(IsRotating)
			{
				_HandleRotate(mm.Relative);
			}
		}
	}


	protected void _HandleKeyboardInput(double delta)
	{
		var move_dir = Vector3.Zero;
		var rotate_dir = 0.0;
		var pitch_dir = 0.0;
		var vertical_dir = 0.0;


		// WASD / Arrow keys for panning
		if(Godot.Input.IsKeyPressed(KEY_W) || Godot.Input.IsKeyPressed(KEY_UP))
		{
			move_dir.Z -= 1;
		}
		if(Godot.Input.IsKeyPressed(KEY_S) || Godot.Input.IsKeyPressed(KEY_DOWN))
		{
			move_dir.Z += 1;
		}
		if(Godot.Input.IsKeyPressed(KEY_A) || Godot.Input.IsKeyPressed(KEY_LEFT))
		{
			move_dir.X -= 1;
		}
		if(Godot.Input.IsKeyPressed(KEY_D) || Godot.Input.IsKeyPressed(KEY_RIGHT))
		{
			move_dir.X += 1;
		}


		// Q/E for rotation
		if(Godot.Input.IsKeyPressed(KEY_Q))
		{
			rotate_dir -= 1;
		}
		if(Godot.Input.IsKeyPressed(KEY_E))
		{
			rotate_dir += 1;
		}


		// R/F for tilt (pitch)
		if(Godot.Input.IsKeyPressed(KEY_R))
		{
			pitch_dir -= 1;
		}
		if(Godot.Input.IsKeyPressed(KEY_F))
		{
			pitch_dir += 1;
		}


		// Z/X for vertical movement
		if(Godot.Input.IsKeyPressed(KEY_Z))
		{
			vertical_dir += 1;
		}
		if(Godot.Input.IsKeyPressed(KEY_X))
		{
			vertical_dir -= 1;
		}


		// Apply movement relative to camera yaw
		if(move_dir.Length() > 0)
		{
			move_dir = move_dir.Normalized();
			var yaw_rad = Mathf.DegToRad(Yaw);
			var forward = new Vector3(Mathf.Sin(yaw_rad), 0, Mathf.Cos(yaw_rad));
			var right = new Vector3(Mathf.Cos(yaw_rad), 0,  - Mathf.Sin(yaw_rad));
			var movement = (forward * move_dir.Z + right * move_dir.X) * KeyboardPanSpeed * delta;
			TargetPosition += movement;
		}


		// Apply rotation
		if(rotate_dir != 0)
		{
			TargetYaw += rotate_dir * KeyboardRotateSpeed * delta;
		}


		// Apply pitch
		if(pitch_dir != 0)
		{
			TargetPitch += pitch_dir * KeyboardRotateSpeed * delta;
			TargetPitch = Mathf.Clamp(TargetPitch, MinPitch, MaxPitch);
		}


		// Apply vertical movement
		if(vertical_dir != 0)
		{
			TargetPosition.Y += vertical_dir * KeyboardPanSpeed * delta;
			TargetPosition.Y = Mathf.Clamp(TargetPosition.Y, 0, 30);
		}
	}


	protected void _HandlePan(Vector2 delta)
	{

		// Pan speed scales with distance
		var scale = Distance * PanSpeed * 0.01;
		var yaw_rad = Mathf.DegToRad(Yaw);


		// Calculate movement in world space relative to camera orientation
		var forward = new Vector3(Mathf.Sin(yaw_rad), 0, Mathf.Cos(yaw_rad));
		var right = new Vector3(Mathf.Cos(yaw_rad), 0,  - Mathf.Sin(yaw_rad));

		TargetPosition -= right * delta.X * Scale;
		TargetPosition -= forward * delta.Y * Scale;
	}


	protected void _HandleRotate(Vector2 delta)
	{

		// Horizontal drag rotates yaw
		TargetYaw -= delta.X * RotateSpeed;


		// Vertical drag adjusts pitch
		TargetPitch += delta.Y * RotateSpeed;
		TargetPitch = Mathf.Clamp(TargetPitch, MinPitch, MaxPitch);
	}


	protected void _ApplySmoothing(double delta)
	{
		var t = 1.0 - Mathf.Pow(0.001, delta * Smoothing);

		Target = Target.Lerp(TargetPosition, t);
		Distance = Mathf.Lerp(Distance, TargetDistance, t);
		Pitch = Mathf.Lerp(Pitch, TargetPitch, t);
		Yaw = Mathf.Lerp(Yaw, TargetYaw, t);
	}


	protected void _UpdateCameraPosition()
	{
		var pitch_rad = Mathf.DegToRad(Pitch);
		var yaw_rad = Mathf.DegToRad(Yaw);


		// Calculate camera position on orbital sphere around target


		var offset = new Vector3(, Mathf.Sin(yaw_rad) * Mathf.Cos(pitch_rad) * Distance, Mathf.Sin(pitch_rad) * Distance, Mathf.Cos(yaw_rad) * Mathf.Cos(pitch_rad) * Distance);

		GlobalPosition = Target + offset;
		LookAt(Target, Vector3.Up);
	}


	//# Set the camera target (what it looks at)
	public void SetTarget(Vector3 new_target)
	{
		TargetPosition = new_target;
		Target = new_target;
	}


	//# Get current target position
	public Vector3 GetTarget()
	{
		return Target;
	}


	//# Focus on a specific world position
	public void FocusOn(Vector3 world_pos, double new_distance =  - 1)
	{
		TargetPosition = world_pos;
		if(new_distance > 0)
		{
			TargetDistance = Mathf.Clamp(new_distance, MinZoom, MaxZoom);
		}
	}


}