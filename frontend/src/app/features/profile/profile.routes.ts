import { Routes } from '@angular/router';

export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./profile-overview/profile-overview.component').then(m => m.ProfileOverviewComponent),
  },
  {
    path: 'watchlist',
    loadComponent: () =>
      import('./watchlist/watchlist.component').then(m => m.WatchlistComponent),
  },
];
