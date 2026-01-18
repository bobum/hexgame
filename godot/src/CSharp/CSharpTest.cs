namespace HexGame;

/// <summary>
/// Test node to verify C# integration with Godot.
/// This can be attached to any Node to verify the C# build works correctly.
/// </summary>
public partial class CSharpTest : Node
{
    public override void _Ready()
    {
        GD.Print("=== HexGame C# Integration Test ===");
        GD.Print($"C# version: {System.Environment.Version}");
        GD.Print($"ServiceLocator initialized: {ServiceLocator.IsInitialized}");
        GD.Print($"Services registered: {ServiceLocator.ServiceCount}");
        GD.Print("C# integration successful!");
        GD.Print("===================================");
    }

    public override void _Process(double delta)
    {
        // This node doesn't need per-frame updates
        // Disable processing after first frame
        SetProcess(false);
    }
}
