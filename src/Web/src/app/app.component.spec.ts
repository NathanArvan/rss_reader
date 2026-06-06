import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the nav with links to All items and Sources', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const nav = fixture.nativeElement as HTMLElement;
    expect(nav.querySelector('.brand')?.textContent).toContain('RSS Reader');
    const linkText = Array.from(nav.querySelectorAll('.links a')).map((a) => a.textContent?.trim());
    expect(linkText).toContain('All items');
    expect(linkText).toContain('Sources');
  });
});
