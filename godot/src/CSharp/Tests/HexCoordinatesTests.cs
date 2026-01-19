namespace HexGame.Tests;

/// <summary>
/// Unit tests for HexCoordinates.
/// These can be run via Godot's built-in test runner or integrated with GUT.
/// </summary>
public partial class HexCoordinatesTests : Node
{
    public override void _Ready()
    {
        GD.Print("=== HexCoordinates Tests ===");
        TestConstruction();
        TestCubeCoordinates();
        TestWorldPositionConversion();
        TestDistanceCalculation();
        TestNeighborCalculation();
        TestEquality();
        GD.Print("=== All HexCoordinates Tests Passed ===");
    }

    private void TestConstruction()
    {
        var coords = new HexCoordinates(5, 10);
        Assert(coords.Q == 5, "Q should be 5");
        Assert(coords.R == 10, "R should be 10");
        GD.Print("  [PASS] Construction");
    }

    private void TestCubeCoordinates()
    {
        var coords = new HexCoordinates(3, 2);
        Assert(coords.X == 3, "X should equal Q");
        Assert(coords.Z == 2, "Z should equal R");
        Assert(coords.Y == -5, "Y should be -Q - R = -5");
        Assert(coords.X + coords.Y + coords.Z == 0, "Cube coordinates should sum to 0");
        GD.Print("  [PASS] Cube coordinates");
    }

    private void TestWorldPositionConversion()
    {
        // Test origin
        var origin = new HexCoordinates(0, 0);
        var worldPos = origin.ToWorldPosition(0);
        Assert(Mathf.IsZeroApprox(worldPos.X), "Origin X should be 0");
        Assert(Mathf.IsZeroApprox(worldPos.Y), "Origin Y at elevation 0 should be 0");
        Assert(Mathf.IsZeroApprox(worldPos.Z), "Origin Z should be 0");

        // Test elevation
        var elevatedPos = origin.ToWorldPosition(5);
        Assert(Mathf.IsEqualApprox(elevatedPos.Y, 5 * HexMetrics.ElevationStep), "Y should match elevation");

        // Test round-trip conversion
        var coords = new HexCoordinates(10, 8);
        var world = coords.ToWorldPosition(0);
        var roundTrip = HexCoordinates.FromWorldPosition(world);
        Assert(roundTrip.Q == coords.Q && roundTrip.R == coords.R, "Round-trip should return same coords");

        GD.Print("  [PASS] World position conversion");
    }

    private void TestDistanceCalculation()
    {
        var a = new HexCoordinates(0, 0);
        var b = new HexCoordinates(0, 0);
        Assert(a.DistanceTo(b) == 0, "Distance to self should be 0");

        // Test adjacent (distance 1)
        var neighbor = new HexCoordinates(1, 0);
        Assert(a.DistanceTo(neighbor) == 1, "Distance to adjacent hex should be 1");

        // Test longer distance
        var far = new HexCoordinates(3, 3);
        Assert(a.DistanceTo(far) == 6, "Distance to (3,3) should be 6");

        GD.Print("  [PASS] Distance calculation");
    }

    private void TestNeighborCalculation()
    {
        var center = new HexCoordinates(5, 5);

        // Test each direction
        var ne = center.GetNeighbor(HexDirection.NE);
        Assert(ne.Q == 6 && ne.R == 5, "NE neighbor should be (6, 5)");

        var sw = center.GetNeighbor(HexDirection.SW);
        Assert(sw.Q == 4 && sw.R == 5, "SW neighbor should be (4, 5)");

        // Test that all neighbors are distance 1
        var neighbors = center.GetNeighbors();
        Assert(neighbors.Length == 6, "Should have 6 neighbors");
        foreach (var n in neighbors)
        {
            Assert(center.DistanceTo(n) == 1, "All neighbors should be distance 1");
        }

        GD.Print("  [PASS] Neighbor calculation");
    }

    private void TestEquality()
    {
        var a = new HexCoordinates(5, 10);
        var b = new HexCoordinates(5, 10);
        var c = new HexCoordinates(5, 11);

        Assert(a == b, "Equal coordinates should be ==");
        Assert(a.Equals(b), "Equal coordinates should be Equals");
        Assert(a != c, "Different coordinates should be !=");
        Assert(a.GetHashCode() == b.GetHashCode(), "Equal coords should have same hash");

        GD.Print("  [PASS] Equality");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
