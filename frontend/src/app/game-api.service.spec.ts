import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GameApiService } from './game-api.service';

describe('GameApiService', () => {
  let service: GameApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(GameApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the selected game mode to the create-game endpoint', () => {
    service.createGame('Computer').subscribe();
    const request = http.expectOne({ method: 'POST', url: '/api/games' });
    expect(request.request.body).toEqual({ mode: 'Computer' });
    request.flush({});
  });

  it('reads and resets the backend scoreboard', () => {
    service.getScoreboard().subscribe();
    http.expectOne({ method: 'GET', url: '/api/scoreboard' }).flush({ xWins: 1, oWins: 0, draws: 0 });
    service.resetScoreboard().subscribe();
    http.expectOne({ method: 'POST', url: '/api/scoreboard/reset' }).flush({ xWins: 0, oWins: 0, draws: 0 });
  });
});
