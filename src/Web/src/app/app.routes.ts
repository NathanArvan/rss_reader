import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/all-items/all-items.component').then((m) => m.AllItemsComponent),
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
  { path: '**', redirectTo: '' },
];
