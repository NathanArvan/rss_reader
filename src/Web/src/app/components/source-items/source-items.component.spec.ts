import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { SourceItemsComponent } from './source-items.component';
import { ItemsService } from '../../services/items.service';
import { Item } from '../../models/api.models';

function item(id: number): Item {
  return { id, sourceId: 3, url: 'https://x', title: `Item ${id}`, fetchedUtc: '', isRead: false };
}

describe('SourceItemsComponent', () => {
  let spy: jasmine.SpyObj<ItemsService>;

  beforeEach(() => {
    spy = jasmine.createSpyObj<ItemsService>('ItemsService', ['listBySource']);
  });

  it('loads the items for the source id in the route', () => {
    spy.listBySource.and.returnValue(of([item(1)]));
    TestBed.configureTestingModule({
      imports: [SourceItemsComponent],
      providers: [
        provideRouter([]),
        { provide: ItemsService, useValue: spy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: '3' }) } },
        },
      ],
    });
    const fixture = TestBed.createComponent(SourceItemsComponent);
    fixture.detectChanges();

    expect(spy.listBySource).toHaveBeenCalledWith(3);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Item 1');
  });
});
