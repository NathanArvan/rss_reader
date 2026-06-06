import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ItemListComponent } from './item-list.component';
import { Item, ItemWithSource } from '../../models/api.models';

function item(partial: Partial<Item> & { id: number }): Item {
  return {
    sourceId: 1,
    url: 'https://x',
    title: `Item ${partial.id}`,
    fetchedUtc: '',
    isRead: false,
    ...partial,
  };
}

describe('ItemListComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ItemListComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders an empty message when there are no items', () => {
    const fixture = TestBed.createComponent(ItemListComponent);
    fixture.componentInstance.items = [];
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.empty')).toBeTruthy();
  });

  it('renders a row per item with an unread dot only for unread items', () => {
    const fixture = TestBed.createComponent(ItemListComponent);
    fixture.componentInstance.items = [
      item({ id: 1, isRead: false }),
      item({ id: 2, isRead: true }),
    ];
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.item').length).toBe(2);
    expect(el.querySelectorAll('.unread-dot').length).toBe(1);
  });

  it('shows the source title when showSource is set', () => {
    const withSource: ItemWithSource = { ...item({ id: 1 }), sourceTitle: 'My Feed' };
    const fixture = TestBed.createComponent(ItemListComponent);
    fixture.componentInstance.items = [withSource];
    fixture.componentInstance.showSource = true;
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.item-source')?.textContent).toContain(
      'My Feed',
    );
  });
});
