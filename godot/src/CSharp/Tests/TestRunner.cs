namespace HexGame.Tests;

/// <summary>
/// Main test runner that executes all C# unit tests.
/// Attach this node to run tests when the scene starts.
/// </summary>
public partial class TestRunner : Node
{
    [Export]
    public bool RunOnStart { get; set; } = true;

    public override void _Ready()
    {
        if (RunOnStart)
        {
            RunAllTests();
        }
    }

    /// <summary>
    /// Runs all test suites.
    /// </summary>
    public void RunAllTests()
    {
        GD.Print("\n");
        GD.Print("╔══════════════════════════════════════════╗");
        GD.Print("║     HexGame C# Unit Tests                ║");
        GD.Print("╚══════════════════════════════════════════╝");
        GD.Print("");

        int passed = 0;
        int failed = 0;

        // Run each test suite
        passed += RunTestSuite<HexCoordinatesTests>("HexCoordinates", ref failed);
        passed += RunTestSuite<HexGridTests>("HexGrid", ref failed);
        passed += RunTestSuite<EventBusTests>("EventBus", ref failed);
        passed += RunTestSuite<CommandHistoryTests>("CommandHistory", ref failed);

        GD.Print("");
        GD.Print("╔══════════════════════════════════════════╗");
        GD.Print($"║  Results: {passed} passed, {failed} failed".PadRight(43) + "║");
        GD.Print("╚══════════════════════════════════════════╝");
        GD.Print("");

        if (failed > 0)
        {
            GD.PrintErr($"TESTS FAILED: {failed} test suite(s) failed");
        }
    }

    private int RunTestSuite<T>(string name, ref int failedCount) where T : Node, new()
    {
        try
        {
            var testNode = new T();
            AddChild(testNode);

            // _Ready will run tests
            // Remove after tests complete
            testNode.QueueFree();

            return 1; // Suite passed
        }
        catch (Exception ex)
        {
            GD.PrintErr($"\n[FAIL] {name} Tests: {ex.Message}");
            GD.PrintErr($"       {ex.StackTrace}");
            failedCount++;
            return 0;
        }
    }
}
