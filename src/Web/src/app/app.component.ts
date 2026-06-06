import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface PingResponse {
  status: string;
  dbConnected: boolean;
  utc: string;
}

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  private readonly http = inject(HttpClient);

  readonly ping = signal<PingResponse | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.http.get<PingResponse>('/api/ping').subscribe({
      next: (res) => this.ping.set(res),
      error: (err) => this.error.set(err.message ?? 'Request failed')
    });
  }
}
