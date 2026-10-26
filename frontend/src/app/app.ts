import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, finalize } from 'rxjs';
import { GameApiService } from './game-api.service';
import { GameMode, GameState, Scoreboard } from './game.models';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly api = inject(GameApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  readonly cells = Array.from({ length: 9 }, (_, index) => index);
  game: GameState | null = null;
  scoreboard: Scoreboard = { xWins: 0, oWins: 0, draws: 0 };
  mode: GameMode = 'TwoPlayer';
  error = '';
  busy = false;
  loginVisible = false;
  username = '';
  password = '';
  authenticated = !!sessionStorage.getItem('tictactoe-token');

  ngOnInit(): void {
    this.startGame();
  }

  startGame(mode: GameMode = this.mode): void {
    this.run(this.api.createGame(mode), (game) => this.acceptGame(game));
  }

  selectMode(mode: GameMode): void {
    if (mode === this.mode) return;
    this.startGame(mode);
  }

  play(cell: number): void {
    if (!this.game || !this.isPlayable(cell)) return;
    this.run(
      this.api.playMove(this.game.gameId, this.game.currentPlayer, Math.floor(cell / 3), cell % 3),
      (game) => this.acceptGame(game),
    );
  }

  undo(): void {
    if (!this.game || !this.canUndo) return;
    this.run(this.api.undo(this.game.gameId), (game) => this.acceptGame(game));
  }

  resetGame(): void {
    if (!this.game) return;
    this.run(this.api.resetGame(this.game.gameId), (game) => this.acceptGame(game));
  }

  resetScoreboard(): void {
    if (!this.authenticated) {
      this.loginVisible = true;
      return;
    }
    this.run(this.api.resetScoreboard(), (scoreboard) => {
      this.scoreboard = scoreboard;
      if (this.game) this.game = { ...this.game, scoreboard };
    });
  }

  login(): void {
    this.run(this.api.login(this.username, this.password), (response) => {
      sessionStorage.setItem('tictactoe-token', response.accessToken);
      this.authenticated = true;
      this.loginVisible = false;
      this.password = '';
      this.error = '';
    });
  }

  logout(): void {
    sessionStorage.removeItem('tictactoe-token');
    this.authenticated = false;
  }

  isPlayable(cell: number): boolean {
    return !!this.game &&
      this.game.status === 'InProgress' &&
      this.game.board[cell] === null &&
      !(this.game.mode === 'Computer' && this.game.currentPlayer === 'O');
  }

  rowNumber(cell: number): number {
    return Math.floor(cell / 3) + 1;
  }

  columnNumber(cell: number): number {
    return cell % 3 + 1;
  }

  get canUndo(): boolean {
    if (!this.game || this.game.status !== 'InProgress') return false;
    return this.game.moveHistory.length >= (this.game.mode === 'Computer' ? 2 : 1);
  }

  get statusMessage(): string {
    if (!this.game) return 'Starting a new game…';
    if (this.game.status === 'Won') return `${this.game.winner} wins!`;
    if (this.game.status === 'Draw') return "It's a draw!";
    if (this.game.mode === 'Computer' && this.game.currentPlayer === 'O') return 'Computer is thinking…';
    return `Player ${this.game.currentPlayer}'s turn`;
  }

  private acceptGame(game: GameState): void {
    this.game = game;
    this.mode = game.mode;
    this.scoreboard = game.scoreboard;
  }

  private run<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    this.busy = true;
    this.error = '';
    request.pipe(finalize(() => {
      this.busy = false;
      this.changeDetector.markForCheck();
    })).subscribe({
      next: (value) => {
        onSuccess(value);
        this.changeDetector.markForCheck();
      },
      error: (error: { error?: { error?: string }; message?: string }) => {
        this.error = error.error?.error ?? error.message ?? 'The request failed. Please try again.';
        this.changeDetector.markForCheck();
      },
    });
  }
}
