import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ItemsService } from './items.service';
import { Item, ItemsPage, TriageState } from '../models/api.models';

function item(partial: Partial<Item> & { id: number; sourceId: number }): Item {
  return {
    url: 'https://x',
    title: `Item ${partial.id}`,
    fetchedUtc: '2026-01-01T00:00:00Z',
    isRead: false,
    triageState: TriageState.New,
    ...partial,
  };
}

function page(items: Item[], hasMore = false, nextCursor: string | null = null): ItemsPage {
  return { items, hasMore, nextCursor };
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

  describe('list()', () => {
    it('calls GET /api/items with no params by default', () => {
      service.list().subscribe();
      const req = httpMock.expectOne('/api/items');
      expect(req.request.method).toBe('GET');
      req.flush(page([]));
    });

    it('serialises triage as repeated query params', () => {
      service.list({ triage: [TriageState.New] }).subscribe();
      const req = httpMock.expectOne((r) => r.url === '/api/items');
      expect(req.request.params.getAll('triage')).toEqual(['0']);
      req.flush(page([]));
    });

    it('serialises multiple triage values', () => {
      service.list({ triage: [TriageState.New, TriageState.Kept] }).subscribe();
      const req = httpMock.expectOne((r) => r.url === '/api/items');
      expect(req.request.params.getAll('triage')).toEqual(['0', '1']);
      req.flush(page([]));
    });

    it('passes cursor and limit', () => {
      service.list({ limit: 25, cursor: 'abc123' }).subscribe();
      const req = httpMock.expectOne((r) => r.url === '/api/items');
      expect(req.request.params.get('limit')).toBe('25');
      expect(req.request.params.get('cursor')).toBe('abc123');
      req.flush(page([]));
    });

    it('returns hasMore=true and a nextCursor when more items exist', () => {
      let result: ItemsPage | undefined;
      service.list().subscribe((r) => (result = r));
      const req = httpMock.expectOne('/api/items');
      req.flush(page([item({ id: 1, sourceId: 1 })], true, 'cursor=='));
      expect(result?.hasMore).toBeTrue();
      expect(result?.nextCursor).toBe('cursor==');
    });

    it('returns hasMore=false and null nextCursor at end of list', () => {
      let result: ItemsPage | undefined;
      service.list().subscribe((r) => (result = r));
      const req = httpMock.expectOne('/api/items');
      req.flush(page([item({ id: 1, sourceId: 1 })], false, null));
      expect(result?.hasMore).toBeFalse();
      expect(result?.nextCursor).toBeNull();
    });
  });

  describe('listBySource()', () => {
    it('calls GET /api/sources/:id/items', () => {
      service.listBySource(3).subscribe();
      const req = httpMock.expectOne((r) => r.url === '/api/sources/3/items');
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });

    it('passes limit and before params', () => {
      service.listBySource(3, { limit: 10, before: '2026-01-01' }).subscribe();
      const req = httpMock.expectOne((r) => r.url === '/api/sources/3/items');
      expect(req.request.params.get('limit')).toBe('10');
      expect(req.request.params.get('before')).toBe('2026-01-01');
      req.flush([]);
    });
  });

  describe('get()', () => {
    it('calls GET /api/items/:id', () => {
      let result: Item | undefined;
      service.get(42).subscribe((r) => (result = r));
      const req = httpMock.expectOne('/api/items/42');
      expect(req.request.method).toBe('GET');
      req.flush(item({ id: 42, sourceId: 1 }));
      expect(result?.id).toBe(42);
    });
  });

  describe('markRead()', () => {
    it('POSTs to /api/items/:id/read with ?read=true by default', () => {
      service.markRead(5).subscribe();
      const req = httpMock.expectOne((r) => r.url === '/api/items/5/read');
      expect(req.request.method).toBe('POST');
      expect(req.request.params.get('read')).toBe('true');
      req.flush(item({ id: 5, sourceId: 1, isRead: true }));
    });

    it('passes ?read=false when read=false', () => {
      service.markRead(5, false).subscribe();
      const req = httpMock.expectOne((r) => r.url === '/api/items/5/read');
      expect(req.request.params.get('read')).toBe('false');
      req.flush(item({ id: 5, sourceId: 1, isRead: false }));
    });
  });

  describe('keep()', () => {
    it('POSTs to /api/items/:id/keep', () => {
      let result: Item | undefined;
      service.keep(7).subscribe((r) => (result = r));
      const req = httpMock.expectOne('/api/items/7/keep');
      expect(req.request.method).toBe('POST');
      req.flush(item({ id: 7, sourceId: 1, triageState: TriageState.Kept }));
      expect(result?.triageState).toBe(TriageState.Kept);
    });
  });

  describe('dismiss()', () => {
    it('POSTs to /api/items/:id/dismiss', () => {
      let result: Item | undefined;
      service.dismiss(8).subscribe((r) => (result = r));
      const req = httpMock.expectOne('/api/items/8/dismiss');
      expect(req.request.method).toBe('POST');
      req.flush(item({ id: 8, sourceId: 1, triageState: TriageState.Dismissed }));
      expect(result?.triageState).toBe(TriageState.Dismissed);
    });
  });

  describe('bulkDismissAll()', () => {
    it('fires dismiss-all for each source in parallel', () => {
      let done = false;
      service.bulkDismissAll([1, 2, 3]).subscribe(() => (done = true));

      httpMock.expectOne('/api/sources/1/items/dismiss-all').flush({ count: 5 });
      httpMock.expectOne('/api/sources/2/items/dismiss-all').flush({ count: 0 });
      httpMock.expectOne('/api/sources/3/items/dismiss-all').flush({ count: 2 });

      expect(done).toBeTrue();
    });

    it('completes immediately with an empty source list', () => {
      let done = false;
      service.bulkDismissAll([]).subscribe(() => (done = true));
      httpMock.expectNone('/api/sources/1/items/dismiss-all');
      expect(done).toBeTrue();
    });
  });
});
