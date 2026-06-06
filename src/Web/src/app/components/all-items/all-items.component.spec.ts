import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AllItemsComponent } from './all-items.component';
import { ItemsService } from '../../services/items.service';
import { ItemWithSource } from '../../models/api.models';

describe('AllItemsComponent', () => {
  let itemsSpy: jasmine.SpyObj<ItemsService>;

  function setup() {
    TestBed.configureTestingModule({
      imports: [AllItemsComponent],
      providers: [provideRouter([]), { provide: ItemsService, useValue: itemsSpy }],
    });
    return TestBed.createComponent(AllItemsComponent);
  }

  beforeEach(() => {
    itemsSpy = jasmine.createSpyObj<ItemsService>('ItemsService', ['listAll']);
  });

  it('renders the combined feed on success', () => {
    const feed: ItemWithSource[] = [
      {
        id: 1,
        sourceId: 1,
        url: 'https://x',
        title: 'Hello',
        fetchedUtc: '',
        isRead: false,
        sourceTitle: 'Feed A',
      },
    ];
    itemsSpy.listAll.and.returnValue(of(feed));
    const fixture = setup();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Hello');
  });

  it('shows an error message when loading fails', () => {
    itemsSpy.listAll.and.returnValue(throwError(() => new Error('boom')));
    const fixture = setup();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.error')?.textContent).toContain('boom');
  });
});
