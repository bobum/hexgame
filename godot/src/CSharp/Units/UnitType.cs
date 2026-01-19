using HexGame.Core;

namespace HexGame.Units;

/// <summary>
/// Domain determines where a unit can move.
/// </summary>
public enum UnitDomain
{
    /// <summary>Can only move on land (elevation >= SeaLevel).</summary>
    Land,
    /// <summary>Can only move on water (elevation < SeaLevel).</summary>
    Naval,
    /// <summary>Can move on both land and water.</summary>
    Amphibious
}

/// <summary>
/// All available unit types.
/// </summary>
public enum UnitType
{
    // Land units
    Infantry,
    Cavalry,
    Archer,
    // Naval units
    Galley,
    Warship,
    // Amphibious units
    Marine
}

/// <summary>
/// Static unit stats configuration.
/// </summary>
public readonly record struct UnitStats(
    UnitDomain Domain,
    int Health,
    int Movement,
    int Attack,
    int Defense,
    string Name,
    string Description
);

/// <summary>
/// Extension methods for UnitType providing stats and domain information.
/// </summary>
public static class UnitTypeExtensions
{
    private static readonly Dictionary<UnitType, UnitStats> Stats = new()
    {
        {
            UnitType.Infantry, new UnitStats(
                UnitDomain.Land,
                Health: GameConstants.UnitStats.InfantryHealth,
                Movement: GameConstants.UnitStats.InfantryMovement,
                Attack: GameConstants.UnitStats.InfantryAttack,
                Defense: GameConstants.UnitStats.InfantryDefense,
                Name: "Infantry",
                Description: "Basic land unit. Slow but sturdy."
            )
        },
        {
            UnitType.Cavalry, new UnitStats(
                UnitDomain.Land,
                Health: GameConstants.UnitStats.CavalryHealth,
                Movement: GameConstants.UnitStats.CavalryMovement,
                Attack: GameConstants.UnitStats.CavalryAttack,
                Defense: GameConstants.UnitStats.CavalryDefense,
                Name: "Cavalry",
                Description: "Fast land unit. Good for flanking."
            )
        },
        {
            UnitType.Archer, new UnitStats(
                UnitDomain.Land,
                Health: GameConstants.UnitStats.ArcherHealth,
                Movement: GameConstants.UnitStats.ArcherMovement,
                Attack: GameConstants.UnitStats.ArcherAttack,
                Defense: GameConstants.UnitStats.ArcherDefense,
                Name: "Archer",
                Description: "Ranged land unit. High attack, low defense."
            )
        },
        {
            UnitType.Galley, new UnitStats(
                UnitDomain.Naval,
                Health: GameConstants.UnitStats.GalleyHealth,
                Movement: GameConstants.UnitStats.GalleyMovement,
                Attack: GameConstants.UnitStats.GalleyAttack,
                Defense: GameConstants.UnitStats.GalleyDefense,
                Name: "Galley",
                Description: "Light naval unit. Fast and maneuverable."
            )
        },
        {
            UnitType.Warship, new UnitStats(
                UnitDomain.Naval,
                Health: GameConstants.UnitStats.WarshipHealth,
                Movement: GameConstants.UnitStats.WarshipMovement,
                Attack: GameConstants.UnitStats.WarshipAttack,
                Defense: GameConstants.UnitStats.WarshipDefense,
                Name: "Warship",
                Description: "Heavy naval unit. Slow but powerful."
            )
        },
        {
            UnitType.Marine, new UnitStats(
                UnitDomain.Amphibious,
                Health: GameConstants.UnitStats.MarineHealth,
                Movement: GameConstants.UnitStats.MarineMovement,
                Attack: GameConstants.UnitStats.MarineAttack,
                Defense: GameConstants.UnitStats.MarineDefense,
                Name: "Marine",
                Description: "Amphibious unit. Can move on land and water."
            )
        }
    };

    /// <summary>
    /// Gets the complete stats for a unit type.
    /// </summary>
    public static UnitStats GetStats(this UnitType type) => Stats[type];

    /// <summary>
    /// Gets the domain (land/naval/amphibious) for a unit type.
    /// </summary>
    public static UnitDomain GetDomain(this UnitType type) => Stats[type].Domain;

    /// <summary>
    /// Gets the display name for a unit type.
    /// </summary>
    public static string GetDisplayName(this UnitType type) => Stats[type].Name;

    /// <summary>
    /// Checks if this unit type can traverse land.
    /// </summary>
    public static bool CanTraverseLand(this UnitType type)
    {
        var domain = type.GetDomain();
        return domain == UnitDomain.Land || domain == UnitDomain.Amphibious;
    }

    /// <summary>
    /// Checks if this unit type can traverse water.
    /// </summary>
    public static bool CanTraverseWater(this UnitType type)
    {
        var domain = type.GetDomain();
        return domain == UnitDomain.Naval || domain == UnitDomain.Amphibious;
    }

    /// <summary>
    /// Gets all land unit types.
    /// </summary>
    public static UnitType[] GetLandTypes() => new[] { UnitType.Infantry, UnitType.Cavalry, UnitType.Archer };

    /// <summary>
    /// Gets all naval unit types.
    /// </summary>
    public static UnitType[] GetNavalTypes() => new[] { UnitType.Galley, UnitType.Warship };

    /// <summary>
    /// Checks if this unit type is naval-only.
    /// </summary>
    public static bool IsNaval(this UnitType type) => type.GetDomain() == UnitDomain.Naval;

    /// <summary>
    /// Checks if this unit type is amphibious.
    /// </summary>
    public static bool IsAmphibious(this UnitType type) => type.GetDomain() == UnitDomain.Amphibious;
}
