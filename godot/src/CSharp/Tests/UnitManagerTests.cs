namespace HexGame.Tests;

using HexGame.Units;

/// <summary>
/// Unit tests for UnitManager.
/// </summary>
public partial class UnitManagerTests : Node
{
    private HexGrid _grid = null!;
    private UnitManager _unitManager = null!;

    public override void _Ready()
    {
        GD.Print("=== UnitManager Tests ===");

        SetupTestGrid();

        TestCreateUnit();
        TestCreateUnitDomainValidation();
        TestMoveUnit();
        TestRemoveUnit();
        TestGetUnitAt();
        TestObjectPooling();
        TestResetMovement();

        GD.Print("=== All UnitManager Tests Passed ===");
    }

    private void SetupTestGrid()
    {
        _grid = new HexGrid(10, 10);
        _grid.Initialize();

        // Set up some land and water cells
        foreach (var cell in _grid.GetAllCells())
        {
            // Make first 3 rows water, rest land
            if (cell.R < 3)
            {
                cell.Elevation = 2; // Water
                cell.TerrainType = TerrainType.Ocean;
            }
            else
            {
                cell.Elevation = 6; // Land
                cell.TerrainType = TerrainType.Plains;
            }
        }

        _unitManager = new UnitManager(_grid);
        _unitManager.SetupPool();
        _unitManager.Initialize();
    }

    private void TestCreateUnit()
    {
        _unitManager.Clear();

        var unit = _unitManager.CreateUnit(UnitType.Infantry, 5, 5, 1);
        Assert(unit != null, "Unit should be created");
        Assert(unit!.Id > 0, "Unit should have valid ID");
        Assert(unit.Q == 5 && unit.R == 5, "Unit should be at correct position");
        Assert(unit.Type == UnitType.Infantry, "Unit should be correct type");
        Assert(unit.PlayerId == 1, "Unit should have correct player ID");
        Assert(_unitManager.UnitCount == 1, "Unit count should be 1");

        GD.Print("  [PASS] Create unit");
    }

    private void TestCreateUnitDomainValidation()
    {
        _unitManager.Clear();

        // Try to create land unit on water - should fail
        var landOnWater = _unitManager.CreateUnit(UnitType.Infantry, 5, 1, 1);
        Assert(landOnWater == null, "Land unit on water should fail");

        // Try to create naval unit on land - should fail
        var navalOnLand = _unitManager.CreateUnit(UnitType.Galley, 5, 5, 1);
        Assert(navalOnLand == null, "Naval unit on land should fail");

        // Create naval unit on water - should succeed
        var navalOnWater = _unitManager.CreateUnit(UnitType.Galley, 5, 1, 1);
        Assert(navalOnWater != null, "Naval unit on water should succeed");

        // Create land unit on land - should succeed
        var landOnLand = _unitManager.CreateUnit(UnitType.Infantry, 5, 5, 1);
        Assert(landOnLand != null, "Land unit on land should succeed");

        GD.Print("  [PASS] Domain validation");
    }

    private void TestMoveUnit()
    {
        _unitManager.Clear();

        var unit = _unitManager.CreateUnit(UnitType.Infantry, 5, 5, 1);
        Assert(unit != null, "Unit should be created");

        // Move the unit
        bool moved = _unitManager.MoveUnit(unit!.Id, 6, 5, 1);
        Assert(moved, "Move should succeed");
        Assert(unit.Q == 6 && unit.R == 5, "Unit should be at new position");

        // Verify old position is empty
        Assert(_unitManager.GetUnitAt(5, 5) == null, "Old position should be empty");

        // Verify new position has unit
        Assert(_unitManager.GetUnitAt(6, 5) == unit, "New position should have unit");

        GD.Print("  [PASS] Move unit");
    }

    private void TestRemoveUnit()
    {
        _unitManager.Clear();

        var unit = _unitManager.CreateUnit(UnitType.Infantry, 5, 5, 1);
        Assert(unit != null, "Unit should be created");
        int unitId = unit!.Id;

        bool removed = _unitManager.RemoveUnit(unitId);
        Assert(removed, "Remove should succeed");
        Assert(_unitManager.GetUnit(unitId) == null, "Unit should no longer exist");
        Assert(_unitManager.GetUnitAt(5, 5) == null, "Position should be empty");
        Assert(_unitManager.UnitCount == 0, "Unit count should be 0");

        GD.Print("  [PASS] Remove unit");
    }

    private void TestGetUnitAt()
    {
        _unitManager.Clear();

        Assert(_unitManager.GetUnitAt(5, 5) == null, "Empty cell should return null");

        var unit = _unitManager.CreateUnit(UnitType.Infantry, 5, 5, 1);
        Assert(_unitManager.GetUnitAt(5, 5) == unit, "Should return correct unit");
        Assert(_unitManager.GetUnitAt(5, 6) == null, "Adjacent cell should return null");

        GD.Print("  [PASS] Get unit at position");
    }

    private void TestObjectPooling()
    {
        _unitManager.Clear();
        _unitManager.PrewarmPool(10);

        var stats1 = _unitManager.GetPoolStats();
        Assert(stats1.Available >= 10, "Pool should have prewarmed units");

        // Create and remove units to test pooling
        var unit = _unitManager.CreateUnit(UnitType.Infantry, 5, 5, 1);
        Assert(unit != null, "Unit should be created");

        _unitManager.RemoveUnit(unit!.Id);

        var stats2 = _unitManager.GetPoolStats();
        Assert(stats2.Available > 0, "Pool should have returned unit");

        GD.Print("  [PASS] Object pooling");
    }

    private void TestResetMovement()
    {
        _unitManager.Clear();

        var unit = _unitManager.CreateUnit(UnitType.Infantry, 5, 5, 1);
        Assert(unit != null, "Unit should be created");

        // Spend movement
        unit!.SpendMovement(1);
        Assert(unit.Movement < unit.MaxMovement, "Movement should be reduced");

        // Reset movement
        _unitManager.ResetAllMovement();
        Assert(unit.Movement == unit.MaxMovement, "Movement should be reset");

        GD.Print("  [PASS] Reset movement");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
