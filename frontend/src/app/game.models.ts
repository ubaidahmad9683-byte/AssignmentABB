export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'Computer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface MoveRecord {
  moveNumber: number;
  player: Player;
  row: number;
  column: number;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameState {
  gameId: string;
  board: (Player | null)[];
  currentPlayer: Player;
  mode: GameMode;
  status: GameStatus;
  winner: Player | null;
  winningCells: number[];
  moveHistory: MoveRecord[];
  scoreboard: Scoreboard;
}
