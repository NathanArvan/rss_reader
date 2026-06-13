import { Routes } from '@angular/router';
import { TriageState } from './models/api.models';

export const routes: Routes = [
  { path: '', redirectTo: 'inbox', pathMatch: 'full' },
  {
    path: 'inbox',
    data: { channel: 'inbox', triage: [TriageState.New] },
    loadComponent: () =>
      import('./features/feed/feed.component').then((m) => m.FeedComponent),
  },
  {
    path: 'interested',
    data: { channel: 'interested', triage: [TriageState.Kept] },
    loadComponent: () =>
      import('./features/feed/feed.component').then((m) => m.FeedComponent),
  },
  {
    path: 'everything',
    data: { channel: 'everything' },
    loadComponent: () =>
      import('./features/feed/feed.component').then((m) => m.FeedComponent),
  },
  {
    path: 'sources',
    loadComponent: () =>
      import('./components/sources/sources.component').then((m) => m.SourcesComponent),
  },
  {
    path: 'sources/:id',
    loadComponent: () =>
      import('./components/source-items/source-items.component').then((m) => m.SourceItemsComponent),
  },
  {
    path: 'items/:id',
    loadComponent: () =>
      import('./components/item-detail/item-detail.component').then((m) => m.ItemDetailComponent),
  },
  { path: '**', redirectTo: 'inbox' },
];
