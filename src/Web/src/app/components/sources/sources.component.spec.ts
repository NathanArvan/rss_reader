import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { SourcesComponent } from './sources.component';
import { SourcesService } from '../../services/sources.service';
import { Source, SourceType } from '../../models/api.models';

function source(id: number, title: string): Source {
  return { id, feedUrl: `https://f/${id}`, title, type: SourceType.Rss, addedUtc: '' };
}

describe('SourcesComponent', () => {
  let spy: jasmine.SpyObj<SourcesService>;

  function setup() {
    TestBed.configureTestingModule({
      imports: [SourcesComponent],
      providers: [provideRouter([]), { provide: SourcesService, useValue: spy }],
    });
    return TestBed.createComponent(SourcesComponent);
  }

  beforeEach(() => {
    spy = jasmine.createSpyObj<SourcesService>('SourcesService', ['list', 'add', 'remove']);
  });

  it('lists sources on load', () => {
    spy.list.and.returnValue(of([source(1, 'Alpha')]));
    const fixture = setup();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Alpha');
  });

  it('adds a feed then reloads the list', () => {
    spy.list.and.returnValues(of([]), of([source(1, 'Alpha')]));
    spy.add.and.returnValue(of(source(1, 'Alpha')));
    const fixture = setup();
    fixture.detectChanges();

    fixture.componentInstance.feedUrl.set('https://new/feed');
    fixture.componentInstance.add();
    fixture.detectChanges();

    expect(spy.add).toHaveBeenCalledWith({ feedUrl: 'https://new/feed' });
    expect(spy.list).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.feedUrl()).toBe('');
  });

  it('surfaces a friendly message on duplicate (409)', () => {
    spy.list.and.returnValue(of([]));
    spy.add.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    const fixture = setup();
    fixture.detectChanges();

    fixture.componentInstance.feedUrl.set('https://dup/feed');
    fixture.componentInstance.add();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toContain('already');
  });
});
