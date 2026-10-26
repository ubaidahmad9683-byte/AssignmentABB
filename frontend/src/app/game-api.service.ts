import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { GameMode, GameState, Player, Scoreboard } from './game.models';

@Injectable({ providedIn: 'root' })
export class GameApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  createGame(mode: GameMode): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games`, { mode });
  }

  login(username: string, password: string): Observable<{ accessToken: string; tokenType: string }> {
    return this.http.post<{ accessToken: string; tokenType: string }>(`${this.baseUrl}/auth/token`, { username, password });
  }

  playMove(gameId: string, player: Player, row: number, column: number): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${gameId}/moves`, { player, row, column });
  }

  undo(gameId: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${gameId}/undo`, {});
  }

  resetGame(gameId: string): Observable<GameState> {
    return this.http.post<GameState>(`${this.baseUrl}/games/${gameId}/reset`, {});
  }

  getScoreboard(): Observable<Scoreboard> {
    return this.http.get<Scoreboard>(`${this.baseUrl}/scoreboard`);
  }

  resetScoreboard(): Observable<Scoreboard> {
    return this.http.post<Scoreboard>(`${this.baseUrl}/scoreboard/reset`, {});
  }
}
