import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ItemsService } from './items.service';
import { Item, ItemWithSource, Source, SourceType } from '../models/api.models';

function item(partial: Partial<Item> & { id: number; sourceId: number }): Item {
  return {
    url: 'https://x',
    title: `Item ${partial.id}`,
    fetchedUtc: '',
    isRead: false,
    ...partial,
  };
}

describe('ItemsService', () => {
  let service: ItemsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ItemsService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ItemsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists items for a source, passing query params', () => {
    service.listBySource(3, { unreadOnly: true, limit: 10 }).subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/sources/3/items');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('unreadOnly')).toBe('true');
    expect(req.request.params.get('limit')).toBe('10');
    req.flush([]);
  });

  it('gets a single item via GET /api/items/:id', () => {
    let result: Item | undefined;
    service.get(42).subscribe((r) => (result = r));
    const req = httpMock.expectOne('/api/items/42');
    expect(req.request.method).toBe('GET');
    req.flush(item({ id: 42, sourceId: 1 }));
    expect(result?.id).toBe(42);
  });

  it('marks an item read via POST with ?read=true', () => {
    service.markRead(5).subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/items/5/read');
    expect(req.request.method).toBe('POST');
    expect(req.request.params.get('read')).toBe('true');
    req.flush(item({ id: 5, sourceId: 1, isRead: true }));
  });

  it('marks an item unread via ?read=false', () => {
    service.markRead(5, false).subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/items/5/read');
    expect(req.request.params.get('read')).toBe('false');
    req.flush(item({ id: 5, sourceId: 1, isRead: false }));
  });

  it('builds the combined feed: merges per-source items, sorted newest first, tagged with source title', () => {
    const sources: Source[] = [
      { id: 1, feedUrl: 'a', title: 'Alpha', type: SourceType.Rss, addedUtc: '' },
      { id: 2, feedUrl: 'b', title: 'Beta', type: SourceType.Rss, addedUtc: '' },
    ];

    let result: ItemWithSource[] | undefined;
    service.listAll().subscribe((r) => (result = r));

    httpMock.expectOne('/api/sources').flush(sources);

    httpMock
      .expectOne((r) => r.url === '/api/sources/1/items')
      .flush([item({ id: 10, sourceId: 1, publishedUtc: '2026-01-01T00:00:00Z' })]);
    httpMock
      .expectOne((r) => r.url === '/api/sources/2/items')
      .flush([item({ id: 20, sourceId: 2, publishedUtc: '2026-03-01T00:00:00Z' })]);

    expect(result?.map((i) => i.id)).toEqual([20, 10]); // newest first across sources
    expect(result?.[0].sourceTitle).toBe('Beta');
    expect(result?.[1].sourceTitle).toBe('Alpha');
  });

  it('returns an empty combined feed when there are no sources', () => {
    let result: ItemWithSource[] | undefined;
    service.listAll().subscribe((r) => (result = r));
    httpMock.expectOne('/api/sources').flush([]);
    expect(result).toEqual([]);
  });
});
