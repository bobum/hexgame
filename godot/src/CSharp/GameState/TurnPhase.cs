namespace HexGame.GameState;

/// <summary>
/// Turn phases in the game.
/// </summary>
public enum TurnPhase
{
    /// <summary>Player can move units.</summary>
    Movement,
    /// <summary>Player can attack (future).</summary>
    Combat,
    /// <summary>Turn is ending.</summary>
    End
}

/// <summary>
/// Extension methods for TurnPhase.
/// </summary>
public static class TurnPhaseExtensions
{
    /// <summary>
    /// Gets the display name for a turn phase.
    /// </summary>
    public static string GetDisplayName(this TurnPhase phase)
    {
        return phase switch
        {
            TurnPhase.Movement => "movement",
            TurnPhase.Combat => "combat",
            TurnPhase.End => "end",
            _ => "unknown"
        };
    }
}
