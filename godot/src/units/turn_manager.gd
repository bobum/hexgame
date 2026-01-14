class_name TurnManager
extends RefCounted
## Manages turn-based gameplay.
## Tracks current turn, current player, and turn phases.
## Matches web/src/units/TurnManager.ts

enum TurnPhase {
	MOVEMENT,    # Player can move units
	COMBAT,      # Player can attack (future)
	END,         # Turn is ending
}

const PLAYER_HUMAN: int = 1
const PLAYER_AI_START: int = 2  # AI players start at ID 2

var current_turn: int = 1
var current_player: int = PLAYER_HUMAN
var current_phase: TurnPhase = TurnPhase.MOVEMENT
var player_count: int = 2  # Human + 1 AI by default

var unit_manager: UnitManager

# Signals for turn events
signal turn_started
signal turn_ended
signal phase_changed(phase: TurnPhase)
signal player_changed(player_id: int)


func _init(p_unit_manager: UnitManager) -> void:
	unit_manager = p_unit_manager


## Get current turn number
func get_turn() -> int:
	return current_turn


## Get current player ID
func get_player() -> int:
	return current_player


## Get current turn phase
func get_phase() -> TurnPhase:
	return current_phase


## Get phase as string
func get_phase_string() -> String:
	match current_phase:
		TurnPhase.MOVEMENT:
			return "movement"
		TurnPhase.COMBAT:
			return "combat"
		TurnPhase.END:
			return "end"
	return "unknown"


## Check if it's the human player's turn
func is_human_turn() -> bool:
	return current_player == PLAYER_HUMAN


## Check if it's an AI player's turn
func is_ai_turn() -> bool:
	return current_player >= PLAYER_AI_START


## Set the number of players (including human)
func set_player_count(count: int) -> void:
	player_count = max(2, count)


## Start a new game (reset to turn 1, player 1)
func start_game() -> void:
	current_turn = 1
	current_player = PLAYER_HUMAN
	current_phase = TurnPhase.MOVEMENT

	# Reset all unit movement
	if unit_manager:
		unit_manager.reset_all_movement()

	turn_started.emit()


## End the current player's turn and advance to next player
func end_turn() -> void:
	turn_ended.emit()

	# Advance to next player
	current_player += 1

	# If we've gone through all players, start new turn
	if current_player > player_count:
		current_turn += 1
		current_player = PLAYER_HUMAN

	# Reset to movement phase
	current_phase = TurnPhase.MOVEMENT

	# Reset movement for the new current player
	if unit_manager:
		unit_manager.reset_player_movement(current_player)

	player_changed.emit(current_player)
	turn_started.emit()


## Advance to the next phase
func advance_phase() -> void:
	if current_phase == TurnPhase.MOVEMENT:
		current_phase = TurnPhase.COMBAT
	elif current_phase == TurnPhase.COMBAT:
		current_phase = TurnPhase.END
		# Auto-end turn when reaching end phase
		end_turn()
		return

	phase_changed.emit(current_phase)


## Check if a unit belongs to the current player
func is_current_player_unit(player_id: int) -> bool:
	return player_id == current_player


## Check if it's the movement phase
func can_move() -> bool:
	return current_phase == TurnPhase.MOVEMENT


## Check if it's the combat phase
func can_attack() -> bool:
	return current_phase == TurnPhase.COMBAT


## Get a summary of current turn state
func get_status() -> String:
	var player_name = "Player" if current_player == PLAYER_HUMAN else "AI %d" % (current_player - 1)
	return "Turn %d - %s (%s)" % [current_turn, player_name, get_phase_string()]
