import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { App } from './app';
import { GameApiService } from './game-api.service';
import { GameState } from './game.models';

describe('App', () => {
  const initialGame: GameState = {
    gameId: 'session-1',
    board: Array(9).fill(null),
    currentPlayer: 'X',
    mode: 'TwoPlayer',
    status: 'InProgress',
    winner: null,
    winningCells: [],
    moveHistory: [],
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
  };
  let api: ReturnType<typeof createApiMock>;

  beforeEach(async () => {
    api = createApiMock(initialGame);
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: GameApiService, useValue: api }],
    }).compileComponents();
  });

  it('creates a game and renders the nine-cell board', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges(false);

    expect(api.createGame).toHaveBeenCalledWith('TwoPlayer');
    expect(fixture.nativeElement.querySelectorAll('.cell').length).toBe(9);
    expect(fixture.nativeElement.textContent).toContain("Player X's turn");
  });

  it('submits the selected row and column and renders the returned state', () => {
    const updatedGame = {
      ...initialGame,
      board: ['X', ...Array(8).fill(null)],
      currentPlayer: 'O' as const,
    };
    api.playMove.mockReturnValue(of(updatedGame));
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges(false);
    fixture.nativeElement.querySelector('.cell').click();
    fixture.detectChanges(false);

    expect(api.playMove).toHaveBeenCalledWith('session-1', 'X', 0, 0);
    expect(fixture.nativeElement.querySelector('.cell').textContent.trim()).toBe('X');
    expect(fixture.nativeElement.textContent).toContain("Player O's turn");
  });

  it('renders the game after the asynchronous startup request completes', async () => {
    const response = new Subject<GameState>();
    api.createGame.mockReturnValue(response);
    const fixture = TestBed.createComponent(App);
    fixture.autoDetectChanges();

    expect(fixture.nativeElement.textContent).toContain('Starting a new game…');

    response.next(initialGame);
    response.complete();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain("Player X's turn");
    expect(fixture.nativeElement.querySelector('.cell').disabled).toBe(false);
    expect(fixture.nativeElement.querySelector('.mode-picker button').disabled).toBe(false);
  });
});

function createApiMock(initialGame: GameState) {
  return {
    createGame: vi.fn(() => of(initialGame)),
    playMove: vi.fn(() => of(initialGame)),
    undo: vi.fn(() => of(initialGame)),
    resetGame: vi.fn(() => of(initialGame)),
    resetScoreboard: vi.fn(() => of({ xWins: 0, oWins: 0, draws: 0 })),
  };
}
