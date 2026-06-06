import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SourcesService } from './sources.service';
import { Source, SourceType } from '../models/api.models';

describe('SourcesService', () => {
  let service: SourcesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SourcesService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SourcesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists sources via GET /api/sources', () => {
    const sources: Source[] = [
      { id: 1, feedUrl: 'https://a/feed', title: 'A', type: SourceType.Rss, addedUtc: '' },
    ];
    let result: Source[] | undefined;
    service.list().subscribe((r) => (result = r));

    const req = httpMock.expectOne('/api/sources');
    expect(req.request.method).toBe('GET');
    req.flush(sources);
    expect(result).toEqual(sources);
  });

  it('adds a source via POST /api/sources with the request body', () => {
    service.add({ feedUrl: 'https://new/feed' }).subscribe();

    const req = httpMock.expectOne('/api/sources');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ feedUrl: 'https://new/feed' });
    req.flush({ id: 9, feedUrl: 'https://new/feed', title: 'New', type: 0, addedUtc: '' });
  });

  it('removes a source via DELETE /api/sources/:id', () => {
    service.remove(7).subscribe();

    const req = httpMock.expectOne('/api/sources/7');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
