import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SettingsService } from './settings.service';
import { AppSettingsResponse } from '../models/api.models';

describe('SettingsService', () => {
  let service: SettingsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SettingsService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SettingsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('retrieves settings via GET /api/settings', () => {
    const resp: AppSettingsResponse = { lastOpenedUtc: '2026-06-01T10:00:00Z' };
    let result: AppSettingsResponse | undefined;
    service.getSettings().subscribe((r) => (result = r));
    const req = httpMock.expectOne('/api/settings');
    expect(req.request.method).toBe('GET');
    req.flush(resp);
    expect(result?.lastOpenedUtc).toBe('2026-06-01T10:00:00Z');
  });

  it('returns null lastOpenedUtc when settings have never been saved', () => {
    let result: AppSettingsResponse | undefined;
    service.getSettings().subscribe((r) => (result = r));
    httpMock.expectOne('/api/settings').flush({ lastOpenedUtc: null });
    expect(result?.lastOpenedUtc).toBeNull();
  });

  it('advances last-opened via POST /api/settings/last-opened', () => {
    let done = false;
    service.advanceLastOpened().subscribe(() => (done = true));
    const req = httpMock.expectOne('/api/settings/last-opened');
    expect(req.request.method).toBe('POST');
    req.flush(null, { status: 204, statusText: 'No Content' });
    expect(done).toBeTrue();
  });
});
