/**
 * Manages turn-based gameplay.
 * Tracks current turn, current player, and turn phases.
 */
import { UnitManager } from './UnitManager';

/**
 * Turn phases within a player's turn.
 */
export enum TurnPhase {
  Movement = 'movement',    // Player can move units
  Combat = 'combat',        // Player can attack (future)
  End = 'end',              // Turn is ending
}

/**
 * Player IDs.
 */
export const PLAYER_HUMAN = 1;
export const PLAYER_AI_START = 2;  // AI players start at ID 2

/**
 * Callback for turn events.
 */
export type TurnEventCallback = () => void;

/**
 * Manages the turn-based game loop.
 */
export class TurnManager {
  private currentTurn = 1;
  private currentPlayer = PLAYER_HUMAN;
  private currentPhase = TurnPhase.Movement;
  private playerCount = 2;  // Human + 1 AI by default

  // Event callbacks
  private onTurnStartCallbacks: TurnEventCallback[] = [];
  private onTurnEndCallbacks: TurnEventCallback[] = [];
  private onPhaseChangeCallbacks: ((phase: TurnPhase) => void)[] = [];
  private onPlayerChangeCallbacks: ((playerId: number) => void)[] = [];

  constructor(private unitManager: UnitManager) {}

  /**
   * Get current turn number.
   */
  get turn(): number {
    return this.currentTurn;
  }

  /**
   * Get current player ID.
   */
  get player(): number {
    return this.currentPlayer;
  }

  /**
   * Get current turn phase.
   */
  get phase(): TurnPhase {
    return this.currentPhase;
  }

  /**
   * Check if it's the human player's turn.
   */
  get isHumanTurn(): boolean {
    return this.currentPlayer === PLAYER_HUMAN;
  }

  /**
   * Check if it's an AI player's turn.
   */
  get isAITurn(): boolean {
    return this.currentPlayer >= PLAYER_AI_START;
  }

  /**
   * Set the number of players (including human).
   */
  setPlayerCount(count: number): void {
    this.playerCount = Math.max(2, count);
  }

  /**
   * Start a new game (reset to turn 1, player 1).
   */
  startGame(): void {
    this.currentTurn = 1;
    this.currentPlayer = PLAYER_HUMAN;
    this.currentPhase = TurnPhase.Movement;

    // Reset all unit movement
    this.unitManager.resetAllMovement();

    // Fire turn start callbacks
    this.fireTurnStart();
  }

  /**
   * End the current player's turn and advance to next player.
   */
  endTurn(): void {
    // Fire turn end callbacks for current player
    this.fireTurnEnd();

    // Advance to next player
    this.currentPlayer++;

    // If we've gone through all players, start new turn
    if (this.currentPlayer > this.playerCount) {
      this.currentTurn++;
      this.currentPlayer = PLAYER_HUMAN;
    }

    // Reset to movement phase
    this.currentPhase = TurnPhase.Movement;

    // Reset movement for the new current player
    this.unitManager.resetPlayerMovement(this.currentPlayer);

    // Fire callbacks
    this.firePlayerChange(this.currentPlayer);
    this.fireTurnStart();
  }

  /**
   * Advance to the next phase.
   */
  advancePhase(): void {
    if (this.currentPhase === TurnPhase.Movement) {
      this.currentPhase = TurnPhase.Combat;
    } else if (this.currentPhase === TurnPhase.Combat) {
      this.currentPhase = TurnPhase.End;
      // Auto-end turn when reaching end phase
      this.endTurn();
      return;
    }

    this.firePhaseChange(this.currentPhase);
  }

  /**
   * Check if a unit belongs to the current player.
   */
  isCurrentPlayerUnit(playerId: number): boolean {
    return playerId === this.currentPlayer;
  }

  /**
   * Check if it's the movement phase.
   */
  canMove(): boolean {
    return this.currentPhase === TurnPhase.Movement;
  }

  /**
   * Check if it's the combat phase.
   */
  canAttack(): boolean {
    return this.currentPhase === TurnPhase.Combat;
  }

  // Event subscription methods

  /**
   * Subscribe to turn start events.
   */
  onTurnStart(callback: TurnEventCallback): void {
    this.onTurnStartCallbacks.push(callback);
  }

  /**
   * Subscribe to turn end events.
   */
  onTurnEnd(callback: TurnEventCallback): void {
    this.onTurnEndCallbacks.push(callback);
  }

  /**
   * Subscribe to phase change events.
   */
  onPhaseChange(callback: (phase: TurnPhase) => void): void {
    this.onPhaseChangeCallbacks.push(callback);
  }

  /**
   * Subscribe to player change events.
   */
  onPlayerChange(callback: (playerId: number) => void): void {
    this.onPlayerChangeCallbacks.push(callback);
  }

  // Private event firing methods

  private fireTurnStart(): void {
    for (const cb of this.onTurnStartCallbacks) {
      cb();
    }
  }

  private fireTurnEnd(): void {
    for (const cb of this.onTurnEndCallbacks) {
      cb();
    }
  }

  private firePhaseChange(phase: TurnPhase): void {
    for (const cb of this.onPhaseChangeCallbacks) {
      cb(phase);
    }
  }

  private firePlayerChange(playerId: number): void {
    for (const cb of this.onPlayerChangeCallbacks) {
      cb(playerId);
    }
  }

  /**
   * Get a summary of current turn state.
   */
  getStatus(): string {
    const playerName = this.currentPlayer === PLAYER_HUMAN ? 'Player' : `AI ${this.currentPlayer - 1}`;
    return `Turn ${this.currentTurn} - ${playerName} (${this.currentPhase})`;
  }
}
