import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { FeedComponent } from './feed.component';
import { ItemsService } from '../../services/items.service';
import { SettingsService } from '../../services/settings.service';
import { SourcesService } from '../../services/sources.service';
import { AppSettingsResponse, Item, ItemsPage, Source, SourceType, TriageState } from '../../models/api.models';

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

const noSettings: AppSettingsResponse = { lastOpenedUtc: null };
const sources: Source[] = [
  { id: 1, feedUrl: 'https://a/feed', title: 'Feed A', type: SourceType.Rss, addedUtc: '' },
  { id: 2, feedUrl: 'https://b/feed', title: 'Feed B', type: SourceType.Rss, addedUtc: '' },
];

function makeSpies() {
  const itemsSpy = jasmine.createSpyObj<ItemsService>('ItemsService', [
    'list', 'keep', 'dismiss', 'bulkDismissAll',
  ]);
  const settingsSpy = jasmine.createSpyObj<SettingsService>('SettingsService', [
    'getSettings', 'advanceLastOpened',
  ]);
  const sourcesSpy = jasmine.createSpyObj<SourcesService>('SourcesService', ['list']);
  settingsSpy.getSettings.and.returnValue(of(noSettings));
  settingsSpy.advanceLastOpened.and.returnValue(of(void 0));
  sourcesSpy.list.and.returnValue(of(sources));
  return { itemsSpy, settingsSpy, sourcesSpy };
}

function setup(
  channel: 'inbox' | 'interested' | 'everything',
  triage: TriageState[] | undefined,
  spies: ReturnType<typeof makeSpies>,
) {
  TestBed.configureTestingModule({
    imports: [FeedComponent],
    providers: [
      provideRouter([]),
      { provide: ItemsService, useValue: spies.itemsSpy },
      { provide: SettingsService, useValue: spies.settingsSpy },
      { provide: SourcesService, useValue: spies.sourcesSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { data: { channel, triage } } },
      },
    ],
  });
  return TestBed.createComponent(FeedComponent);
}

describe('FeedComponent', () => {
  describe('Inbox channel', () => {
    it('renders items from GET /api/items?triage=New', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(
        of(page([item({ id: 1, sourceId: 1 }), item({ id: 2, sourceId: 1 })])),
      );
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();

      expect(spies.itemsSpy.list).toHaveBeenCalledWith(
        jasmine.objectContaining({ triage: [TriageState.New] }),
      );
      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelectorAll('.item').length).toBe(2);
    });

    it('advances last-opened after loading', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(of(page([])));
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();
      expect(spies.settingsSpy.advanceLastOpened).toHaveBeenCalled();
    });

    it('shows the bulk-dismiss button when there are items', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(of(page([item({ id: 1, sourceId: 1 })])));
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();
      const btn = (fixture.nativeElement as HTMLElement).querySelector('.bulk-dismiss');
      expect(btn).toBeTruthy();
    });

    it('hides the bulk-dismiss button when inbox is empty', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(of(page([])));
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('.bulk-dismiss')).toBeNull();
    });

    it('shows "Nothing new." empty state', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(of(page([])));
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nothing new.');
    });

    it('inserts a new-since divider before the first item at or before lastOpenedUtc', () => {
      const spies = makeSpies();
      spies.settingsSpy.getSettings.and.returnValue(
        of({ lastOpenedUtc: '2026-06-10T12:00:00Z' }),
      );
      spies.itemsSpy.list.and.returnValue(
        of(
          page([
            item({ id: 1, sourceId: 1, publishedUtc: '2026-06-12T00:00:00Z' }), // new
            item({ id: 2, sourceId: 1, publishedUtc: '2026-06-09T00:00:00Z' }), // older — divider before this
          ]),
        ),
      );
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();
      const el = fixture.nativeElement as HTMLElement;
      const divider = el.querySelector('.since-divider');
      expect(divider).toBeTruthy();
      // Divider should appear between item 1 (new) and item 2 (older)
      const listItems = el.querySelectorAll('li');
      const dividerIndex = Array.from(listItems).indexOf(divider as HTMLLIElement);
      expect(dividerIndex).toBe(1); // after first item, before second
    });

    it('does not show a divider when lastOpenedUtc is null', () => {
      const spies = makeSpies();
      spies.settingsSpy.getSettings.and.returnValue(of({ lastOpenedUtc: null }));
      spies.itemsSpy.list.and.returnValue(
        of(page([item({ id: 1, sourceId: 1, publishedUtc: '2026-06-01T00:00:00Z' })])),
      );
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('.since-divider')).toBeNull();
    });

    it('removes item from list on keep', () => {
      const spies = makeSpies();
      const i1 = item({ id: 1, sourceId: 1 });
      spies.itemsSpy.list.and.returnValue(of(page([i1, item({ id: 2, sourceId: 1 })])));
      spies.itemsSpy.keep.and.returnValue(of({ ...i1, triageState: TriageState.Kept }));
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();

      fixture.componentInstance.onKeep(i1);
      fixture.detectChanges();

      expect(fixture.componentInstance.items().length).toBe(1);
      expect(fixture.componentInstance.items()[0].id).toBe(2);
    });

    it('removes item from list on dismiss', () => {
      const spies = makeSpies();
      const i1 = item({ id: 1, sourceId: 1 });
      spies.itemsSpy.list.and.returnValue(of(page([i1, item({ id: 2, sourceId: 1 })])));
      spies.itemsSpy.dismiss.and.returnValue(of({ ...i1, triageState: TriageState.Dismissed }));
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();

      fixture.componentInstance.onDismiss(i1);
      fixture.detectChanges();

      expect(fixture.componentInstance.items().length).toBe(1);
    });

    it('clears all items on bulk dismiss', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(
        of(page([item({ id: 1, sourceId: 1 }), item({ id: 2, sourceId: 2 })])),
      );
      spies.itemsSpy.bulkDismissAll.and.returnValue(of(void 0));
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();

      fixture.componentInstance.onBulkDismiss();
      fixture.detectChanges();

      expect(spies.itemsSpy.bulkDismissAll).toHaveBeenCalledWith([1, 2]);
      expect(fixture.componentInstance.items().length).toBe(0);
      expect(fixture.componentInstance.hasMore()).toBeFalse();
    });
  });

  describe('Pagination (load-older)', () => {
    it('shows "Load older items" button when hasMore=true', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(
        of(page([item({ id: 1, sourceId: 1 })], true, 'cursor==')),
      );
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();
      const btn = (fixture.nativeElement as HTMLElement).querySelector('.load-older');
      expect(btn).toBeTruthy();
    });

    it('shows end-of-items marker when hasMore=false', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(of(page([item({ id: 1, sourceId: 1 })], false)));
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('.end-marker')).toBeTruthy();
    });

    it('appends items and updates hasMore on loadOlder()', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValues(
        of(page([item({ id: 1, sourceId: 1 })], true, 'cur==')),
        of(page([item({ id: 2, sourceId: 1 })], false, null)),
      );
      const fixture = setup('inbox', [TriageState.New], spies);
      fixture.detectChanges();

      fixture.componentInstance.loadOlder();
      fixture.detectChanges();

      expect(fixture.componentInstance.items().length).toBe(2);
      expect(fixture.componentInstance.hasMore()).toBeFalse();
      expect(spies.itemsSpy.list.calls.mostRecent().args[0]).toEqual(
        jasmine.objectContaining({ cursor: 'cur==' }),
      );
    });
  });

  describe('Interested channel', () => {
    it('queries with triage=Kept and shows "Nothing kept yet." when empty', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(of(page([])));
      const fixture = setup('interested', [TriageState.Kept], spies);
      fixture.detectChanges();

      expect(spies.itemsSpy.list).toHaveBeenCalledWith(
        jasmine.objectContaining({ triage: [TriageState.Kept] }),
      );
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nothing kept yet.');
    });

    it('does not show bulk-dismiss button', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(of(page([item({ id: 1, sourceId: 1, triageState: TriageState.Kept })])));
      const fixture = setup('interested', [TriageState.Kept], spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('.bulk-dismiss')).toBeNull();
    });

    it('does not show action buttons for items', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(
        of(page([item({ id: 1, sourceId: 1, triageState: TriageState.Kept })])),
      );
      const fixture = setup('interested', [TriageState.Kept], spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('.item-actions')).toBeNull();
    });
  });

  describe('Everything channel', () => {
    it('queries with no triage filter', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(of(page([])));
      const fixture = setup('everything', undefined, spies);
      fixture.detectChanges();
      const callArgs = spies.itemsSpy.list.calls.mostRecent().args[0] as object;
      expect('triage' in callArgs ? (callArgs as { triage?: unknown }).triage : undefined).toBeFalsy();
    });

    it('shows source title for each item', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(
        of(page([item({ id: 1, sourceId: 1 })])),
      );
      const fixture = setup('everything', undefined, spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('.item-source')?.textContent).toContain('Feed A');
    });

    it('shows keep button for New items', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(
        of(page([item({ id: 1, sourceId: 1, triageState: TriageState.New })])),
      );
      const fixture = setup('everything', undefined, spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('.action-keep')).toBeTruthy();
    });

    it('does not show keep button for already-Kept items', () => {
      const spies = makeSpies();
      spies.itemsSpy.list.and.returnValue(
        of(page([item({ id: 1, sourceId: 1, triageState: TriageState.Kept })])),
      );
      const fixture = setup('everything', undefined, spies);
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('.action-keep')).toBeNull();
    });

    it('updates item in-place on keep (no removal)', () => {
      const spies = makeSpies();
      const i1 = item({ id: 1, sourceId: 1, triageState: TriageState.New });
      spies.itemsSpy.list.and.returnValue(
        of(page([i1, item({ id: 2, sourceId: 1 })])),
      );
      spies.itemsSpy.keep.and.returnValue(of({ ...i1, triageState: TriageState.Kept }));
      const fixture = setup('everything', undefined, spies);
      fixture.detectChanges();

      fixture.componentInstance.onKeep(i1);
      fixture.detectChanges();

      expect(fixture.componentInstance.items().length).toBe(2);
      expect(fixture.componentInstance.items()[0].triageState).toBe(TriageState.Kept);
    });
  });
});
