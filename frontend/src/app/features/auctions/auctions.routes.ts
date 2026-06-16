import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const AUCTIONS_ROUTES: Routes = [
  {
    path: 'create',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./auction-create/auction-create.component').then(m => m.AuctionCreateComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./auction-detail/auction-detail.component').then(m => m.AuctionDetailComponent),
  },
];
