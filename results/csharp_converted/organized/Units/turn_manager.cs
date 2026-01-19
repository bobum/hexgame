using Godot;
using Godot.Collections;


//# Manages turn-based gameplay.
//# Tracks current turn, current player, and turn phases.

//# Matches web/src/units/TurnManager.ts
[GlobalClass]
public partial class TurnManager : Godot.RefCounted
{
	public enum TurnPhase {MOVEMENT,  Player can move units, COMBAT,  Player can attack (future), END,  Turn is ending}

	public const int PLAYER_HUMAN = 1;
	public const int PLAYER_AI_START = 2;

	// AI players start at ID 2
	public int CurrentTurn = 1;
	public int CurrentPlayer = PLAYER_HUMAN;
	public TurnPhase CurrentPhase = TurnPhase.Movement;
	public int PlayerCount = 2;

	// Human + 1 AI by default
	public Godot.UnitManager UnitManager;


	// Signals for turn events
	[Signal]
	public delegate void TurnStartedEventHandler();
	[Signal]
	public delegate void TurnEndedEventHandler();
	[Signal]
	public delegate void PhaseChangedEventHandler(TurnPhase phase);
	[Signal]
	public delegate void PlayerChangedEventHandler(int player_id);


	public override void _Init(Godot.UnitManager p_unit_manager)
	{
		UnitManager = p_unit_manager;
	}


	//# Get current turn number
	public int GetTurn()
	{
		return CurrentTurn;
	}


	//# Get current player ID
	public int GetPlayer()
	{
		return CurrentPlayer;
	}


	//# Get current turn phase
	public TurnPhase GetPhase()
	{
		return CurrentPhase;
	}


	//# Get phase as string
	public String GetPhaseString()
	{

		if(CurrentPhase == TurnPhase.Movement)
		{
			return "movement";
		}
		if(CurrentPhase == TurnPhase.Combat)
		{
			return "combat";
		}
		if(CurrentPhase == TurnPhase.End)
		{
			return "end";
		}
	}
	return "unknown";
}


//# Check if it's the human player's turn
public bool IsHumanTurn()
{
	return CurrentPlayer == PLAYER_HUMAN;
}


//# Check if it's an AI player's turn
public bool IsAiTurn()
{
	return CurrentPlayer >= PLAYER_AI_START;
}


//# Set the number of players (including human)
public void SetPlayerCount(int count)
{
	PlayerCount = Mathf.Max(2, count);
}


//# Start a new game (reset to turn 1, player 1)
public void StartGame()
{
	CurrentTurn = 1;
	CurrentPlayer = PLAYER_HUMAN;
	CurrentPhase = TurnPhase.Movement;


	// Reset all unit movement
	if(UnitManager)
	{
		UnitManager.ResetAllMovement();
	}

	EmitSignal("TurnStarted");
}


//# End the current player's turn and advance to next player
public void EndTurn()
{
	EmitSignal("TurnEnded");


	// Advance to next player
	CurrentPlayer += 1;


	// If we've gone through all players, start new turn
	if(CurrentPlayer > PlayerCount)
	{
		CurrentTurn += 1;
		CurrentPlayer = PLAYER_HUMAN;
	}


	// Reset to movement phase
	CurrentPhase = TurnPhase.Movement;


	// Reset movement for the new current player
	if(UnitManager)
	{
		UnitManager.ResetPlayerMovement(CurrentPlayer);
	}

	EmitSignal("PlayerChanged", CurrentPlayer);
	EmitSignal("TurnStarted");
}


//# Advance to the next phase
public void AdvancePhase()
{
	if(CurrentPhase == TurnPhase.Movement)
	{
		CurrentPhase = TurnPhase.Combat;
	}
	else if(CurrentPhase == TurnPhase.Combat)
	{
		CurrentPhase = TurnPhase.End;

		// Auto-end turn when reaching end phase
		EndTurn();
		return ;
	}

	EmitSignal("PhaseChanged", CurrentPhase);
}


//# Check if a unit belongs to the current player
public bool IsCurrentPlayerUnit(int player_id)
{
	return player_id == CurrentPlayer;
}


//# Check if it's the movement phase
public bool CanMove()
{
	return CurrentPhase == TurnPhase.Movement;
}


//# Check if it's the combat phase
public bool CanAttack()
{
	return CurrentPhase == TurnPhase.Combat;
}


//# Get a summary of current turn state
public String GetStatus()
{
	var player_name = ( CurrentPlayer == PLAYER_HUMAN ? "Player" : "AI %d" % (CurrentPlayer - 1) );
	return "Turn %d - %s (%s)" % new Array{CurrentTurn, player_name, GetPhaseString(), };
}

