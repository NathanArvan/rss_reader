import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { SourcesService } from '../../services/sources.service';
import { Source } from '../../models/api.models';

/** Manage feeds: add by URL, list, and remove. */
@Component({
  selector: 'app-sources',
  imports: [FormsModule, RouterLink],
  templateUrl: './sources.component.html',
  styleUrl: './sources.component.scss',
})
export class SourcesComponent implements OnInit {
  private readonly sources = inject(SourcesService);

  readonly list = signal<Source[] | null>(null);
  readonly feedUrl = signal('');
  readonly adding = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.sources.list().subscribe({
      next: (s) => this.list.set(s),
      error: (err) => this.error.set(err.message ?? 'Could not load sources'),
    });
  }

  add(): void {
    const url = this.feedUrl().trim();
    if (!url || this.adding()) return;
    this.adding.set(true);
    this.error.set(null);

    this.sources.add({ feedUrl: url }).subscribe({
      next: () => {
        this.feedUrl.set('');
        this.adding.set(false);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.adding.set(false);
        this.error.set(this.messageFor(err));
      },
    });
  }

  remove(source: Source): void {
    if (!confirm(`Remove "${source.title}" and all its items?`)) return;
    this.sources.remove(source.id).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err.message ?? 'Could not remove source'),
    });
  }

  private messageFor(err: HttpErrorResponse): string {
    if (err.status === 409) return 'That feed is already in your list.';
    if (err.status === 400) return 'Please enter a valid feed URL.';
    return err.message ?? 'Could not add source';
  }
}
