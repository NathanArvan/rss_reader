import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateSourceRequest, Source } from '../models/api.models';

/** Thin wrapper over the /api/sources endpoints. */
@Injectable({ providedIn: 'root' })
export class SourcesService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/sources';

  list(): Observable<Source[]> {
    return this.http.get<Source[]>(this.base);
  }

  add(request: CreateSourceRequest): Observable<Source> {
    return this.http.post<Source>(this.base, request);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
