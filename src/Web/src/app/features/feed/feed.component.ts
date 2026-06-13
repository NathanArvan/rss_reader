import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Item, Source, TriageState } from '../../models/api.models';
import { ItemsService } from '../../services/items.service';
import { SettingsService } from '../../services/settings.service';
import { SourcesService } from '../../services/sources.service';

type Channel = 'inbox' | 'interested' | 'everything';

@Component({
  selector: 'app-feed',
  imports: [DatePipe, RouterLink],
  templateUrl: './feed.component.html',
  styleUrl: './feed.component.scss',
})
export class FeedComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly itemsSvc = inject(ItemsService);
  private readonly settingsSvc = inject(SettingsService);
  private readonly sourcesSvc = inject(SourcesService);

  channel: Channel = 'inbox';
  triage?: TriageState[];

  readonly items = signal<Item[]>([]);
  readonly hasMore = signal(false);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly lastOpenedUtc = signal<string | null>(null);
  readonly sourceList = signal<Source[]>([]);
  readonly bulkDismissing = signal(false);

  private nextCursor: string | null = null;

  readonly sourceMap = computed<Map<number, string>>(() => {
    const m = new Map<number, string>();
    for (const s of this.sourceList()) m.set(s.id, s.title);
    return m;
  });

  /**
   * Index of the first item at or before lastOpenedUtc — the divider renders
   * before this item. -1 when no divider should appear.
   */
  readonly dividerIndex = computed<number>(() => {
    if (this.channel !== 'inbox') return -1;
    const threshold = this.lastOpenedUtc();
    if (!threshold) return -1;
    const ts = Date.parse(threshold);
    return this.items().findIndex((item) => {
      const d = item.publishedUtc ?? item.fetchedUtc;
      return Date.parse(d) <= ts;
    });
  });

  get channelTitle(): string {
    return this.channel === 'inbox' ? 'Inbox'
      : this.channel === 'interested' ? 'Interested'
      : 'Everything';
  }

  get emptyMessage(): string {
    return this.channel === 'inbox' ? 'Nothing new.'
      : this.channel === 'interested' ? 'Nothing kept yet.'
      : 'No items yet.';
  }

  /** Show the new-since divider before this item? */
  showDivider(index: number): boolean {
    return this.dividerIndex() === index;
  }

  canKeep(item: Item): boolean {
    if (this.channel === 'inbox') return true;
    if (this.channel === 'everything') return item.triageState === TriageState.New;
    return false;
  }

  canDismiss(item: Item): boolean {
    return this.channel === 'inbox';
  }

  sourceTitle(item: Item): string | null {
    return this.channel === 'everything' ? (this.sourceMap().get(item.sourceId) ?? null) : null;
  }

  ngOnInit(): void {
    const data = this.route.snapshot.data;
    this.channel = data['channel'] as Channel;
    this.triage = data['triage'] as TriageState[] | undefined;

    if (this.channel === 'inbox') {
      this.initInbox();
    } else {
      this.initFeed();
    }
  }

  private initInbox(): void {
    forkJoin({
      settings: this.settingsSvc.getSettings(),
      page: this.itemsSvc.list({ triage: this.triage }),
      sources: this.sourcesSvc.list(),
    }).subscribe({
      next: ({ settings, page, sources }) => {
        this.lastOpenedUtc.set(settings.lastOpenedUtc);
        this.items.set(page.items);
        this.hasMore.set(page.hasMore);
        this.nextCursor = page.nextCursor;
        this.sourceList.set(sources);
        this.loading.set(false);
        // Advance the timestamp now so the NEXT visit uses today as its baseline.
        this.settingsSvc.advanceLastOpened().subscribe();
      },
      error: (err) => {
        this.loadError.set(err.message ?? 'Could not load items');
        this.loading.set(false);
      },
    });
  }

  private initFeed(): void {
    const page$ = this.itemsSvc.list({ triage: this.triage });
    const sources$ = this.sourcesSvc.list();
    forkJoin({ page: page$, sources: sources$ }).subscribe({
      next: ({ page, sources }) => {
        this.items.set(page.items);
        this.hasMore.set(page.hasMore);
        this.nextCursor = page.nextCursor;
        this.sourceList.set(sources);
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(err.message ?? 'Could not load items');
        this.loading.set(false);
      },
    });
  }

  loadOlder(): void {
    if (!this.hasMore() || this.loading()) return;
    this.loading.set(true);
    this.itemsSvc.list({ triage: this.triage, cursor: this.nextCursor ?? undefined }).subscribe({
      next: (page) => {
        this.items.update((prev) => [...prev, ...page.items]);
        this.hasMore.set(page.hasMore);
        this.nextCursor = page.nextCursor;
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(err.message ?? 'Could not load items');
        this.loading.set(false);
      },
    });
  }

  onKeep(item: Item): void {
    this.itemsSvc.keep(item.id).subscribe({
      next: (updated) => {
        if (this.channel === 'inbox') {
          this.items.update((prev) => prev.filter((i) => i.id !== updated.id));
        } else {
          this.items.update((prev) => prev.map((i) => (i.id === updated.id ? updated : i)));
        }
      },
    });
  }

  onDismiss(item: Item): void {
    this.itemsSvc.dismiss(item.id).subscribe({
      next: (updated) => {
        this.items.update((prev) => prev.filter((i) => i.id !== updated.id));
      },
    });
  }

  onBulkDismiss(): void {
    if (this.bulkDismissing()) return;
    this.bulkDismissing.set(true);
    const ids = this.sourceList().map((s) => s.id);
    this.itemsSvc.bulkDismissAll(ids).subscribe({
      next: () => {
        this.items.set([]);
        this.hasMore.set(false);
        this.nextCursor = null;
        this.bulkDismissing.set(false);
      },
      error: () => {
        this.bulkDismissing.set(false);
      },
    });
  }
}
