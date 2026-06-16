import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const APP_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/auctions/auction-list/auction-list.component')
        .then(m => m.AuctionListComponent),
  },
  {
    path: 'auctions',
    loadChildren: () =>
      import('./features/auctions/auctions.routes')
        .then(m => m.AUCTIONS_ROUTES),
  },
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes')
        .then(m => m.AUTH_ROUTES),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/profile/profile.routes')
        .then(m => m.PROFILE_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
