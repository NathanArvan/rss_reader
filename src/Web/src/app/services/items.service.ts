import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, forkJoin, map } from 'rxjs';
import { Item, ItemsPage, ItemsPageParams, TriageState } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ItemsService {
  private readonly http = inject(HttpClient);

  /** Cross-source paginated item query — the primary feed method. */
  list(params: ItemsPageParams = {}): Observable<ItemsPage> {
    let p = new HttpParams();
    if (params.triage?.length) {
      for (const t of params.triage) {
        p = p.append('triage', t);
      }
    }
    if (params.sourceId !== undefined) p = p.set('sourceId', params.sourceId);
    if (params.limit !== undefined) p = p.set('limit', params.limit);
    if (params.cursor !== undefined) p = p.set('cursor', params.cursor);
    return this.http.get<ItemsPage>('/api/items', { params: p });
  }

  /** Per-source item list — used by the source-detail view. */
  listBySource(sourceId: number, opts: { limit?: number; before?: string } = {}): Observable<Item[]> {
    let p = new HttpParams();
    if (opts.limit !== undefined) p = p.set('limit', opts.limit);
    if (opts.before !== undefined) p = p.set('before', opts.before);
    return this.http.get<Item[]>(`/api/sources/${sourceId}/items`, { params: p });
  }

  get(id: number): Observable<Item> {
    return this.http.get<Item>(`/api/items/${id}`);
  }

  markRead(id: number, read = true): Observable<Item> {
    const p = new HttpParams().set('read', read);
    return this.http.post<Item>(`/api/items/${id}/read`, null, { params: p });
  }

  keep(id: number): Observable<Item> {
    return this.http.post<Item>(`/api/items/${id}/keep`, null);
  }

  dismiss(id: number): Observable<Item> {
    return this.http.post<Item>(`/api/items/${id}/dismiss`, null);
  }

  /**
   * Dismiss all New items across every source in one parallel batch.
   * Uses POST /api/sources/{id}/items/dismiss-all for each source.
   * Kept items are left untouched by the backend endpoint.
   */
  bulkDismissAll(sourceIds: number[]): Observable<void> {
    if (sourceIds.length === 0) {
      return new Observable((s) => { s.next(void 0); s.complete(); });
    }
    return forkJoin(
      sourceIds.map((id) =>
        this.http.post<{ count: number }>(`/api/sources/${id}/items/dismiss-all`, null),
      ),
    ).pipe(map(() => void 0));
  }
}
