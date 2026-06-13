import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { ItemDetailComponent } from './item-detail.component';
import { ItemsService } from '../../services/items.service';
import { Item, TriageState } from '../../models/api.models';

function item(partial: Partial<Item> & { id: number }): Item {
  return {
    sourceId: 1,
    url: 'https://x',
    title: `Item ${partial.id}`,
    fetchedUtc: '',
    isRead: false,
    triageState: TriageState.New,
    ...partial,
  };
}

describe('ItemDetailComponent', () => {
  let spy: jasmine.SpyObj<ItemsService>;

  function setup(routeId: string) {
    TestBed.configureTestingModule({
      imports: [ItemDetailComponent],
      providers: [
        { provide: ItemsService, useValue: spy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: routeId }) } },
        },
      ],
    });
    return TestBed.createComponent(ItemDetailComponent);
  }

  beforeEach(() => {
    spy = jasmine.createSpyObj<ItemsService>('ItemsService', ['get', 'markRead']);
  });

  it('auto-marks an unread item read on open', () => {
    spy.get.and.returnValue(of(item({ id: 7, isRead: false })));
    spy.markRead.and.returnValue(of(item({ id: 7, isRead: true })));
    const fixture = setup('7');
    fixture.detectChanges();

    expect(spy.markRead).toHaveBeenCalledWith(7, true);
    expect(fixture.componentInstance.item()?.isRead).toBeTrue();
  });

  it('does not auto-mark an item that is already read', () => {
    spy.get.and.returnValue(of(item({ id: 7, isRead: true })));
    const fixture = setup('7');
    fixture.detectChanges();
    expect(spy.markRead).not.toHaveBeenCalled();
  });

  it('renders the summary HTML and a link to the original', () => {
    spy.get.and.returnValue(
      of(item({ id: 7, isRead: true, summary: '<p>Body text</p>', url: 'https://orig' })),
    );
    const fixture = setup('7');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.summary')?.textContent).toContain('Body text');
    expect(el.querySelector('a.open')?.getAttribute('href')).toBe('https://orig');
  });

  it('toggles read state via the button', () => {
    spy.get.and.returnValue(of(item({ id: 7, isRead: true })));
    spy.markRead.and.returnValue(of(item({ id: 7, isRead: false })));
    const fixture = setup('7');
    fixture.detectChanges();

    fixture.componentInstance.toggleRead();
    expect(spy.markRead).toHaveBeenCalledWith(7, false);
  });
});
