import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppSettingsResponse } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);

  getSettings(): Observable<AppSettingsResponse> {
    return this.http.get<AppSettingsResponse>('/api/settings');
  }

  /** Advances the last-opened timestamp to now. Call when the user starts a reading session. */
  advanceLastOpened(): Observable<void> {
    return this.http.post<void>('/api/settings/last-opened', null);
  }
}
