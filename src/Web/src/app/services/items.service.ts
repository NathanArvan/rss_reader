import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { Item, ItemWithSource, ListItemsOptions } from '../models/api.models';
import { SourcesService } from './sources.service';

/** Wrapper over item-related endpoints, plus the client-side combined feed. */
@Injectable({ providedIn: 'root' })
export class ItemsService {
  private readonly http = inject(HttpClient);
  private readonly sources = inject(SourcesService);

  listBySource(sourceId: number, opts: ListItemsOptions = {}): Observable<Item[]> {
    let params = new HttpParams();
    if (opts.unreadOnly !== undefined) params = params.set('unreadOnly', opts.unreadOnly);
    if (opts.limit !== undefined) params = params.set('limit', opts.limit);
    if (opts.before !== undefined) params = params.set('before', opts.before);
    return this.http.get<Item[]>(`/api/sources/${sourceId}/items`, { params });
  }

  /**
   * Combined "All items" feed: fetch every source, pull each one's items, then
   * merge and sort by publishedUtc descending. There is no backend "all items"
   * endpoint yet, so the merge happens here.
   */
  listAll(opts: ListItemsOptions = {}): Observable<ItemWithSource[]> {
    return this.sources.list().pipe(
      switchMap((sources) => {
        if (sources.length === 0) return of([] as ItemWithSource[]);
        const perSource = sources.map((source) =>
          this.listBySource(source.id, opts).pipe(
            map((items) => items.map((item) => ({ ...item, sourceTitle: source.title }))),
          ),
        );
        return forkJoin(perSource).pipe(
          map((lists) => lists.flat().sort(byPublishedDesc)),
        );
      }),
    );
  }

  get(id: number): Observable<Item> {
    return this.http.get<Item>(`/api/items/${id}`);
  }

  markRead(id: number, read = true): Observable<Item> {
    const params = new HttpParams().set('read', read);
    return this.http.post<Item>(`/api/items/${id}/read`, null, { params });
  }
}

/** Newest first; items without a published date sort last. */
function byPublishedDesc(a: ItemWithSource, b: ItemWithSource): number {
  const at = a.publishedUtc ? Date.parse(a.publishedUtc) : 0;
  const bt = b.publishedUtc ? Date.parse(b.publishedUtc) : 0;
  return bt - at;
}
