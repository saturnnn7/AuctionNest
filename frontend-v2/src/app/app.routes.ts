import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';
import { guestGuard } from '@core/guards/guest.guard';


export const routes: Routes = [
    {
        path: '',
        loadComponent: () =>
            import('./features/auctions/auction-list/auction-list.component')
                .then(m => m.AuctionListComponent),
    },
    {
        path: 'auctions/create',
        loadComponent: () =>
            import('./features/auctions/auction-create/auction-create.component')
                .then(m => m.AuctionCreateComponent),
        canActivate: [authGuard],
    },
    {
        path: 'auctions/:id',
        loadComponent: () =>
            import('./features/auctions/auction-detail/auction-detail.component')
                .then(m => m.AuctionDetailComponent),
    },
    {
        path: 'auth/login',
        loadComponent: () =>
            import('./features/auth/login/login.component')
                .then(m => m.LoginComponent),
        canActivate: [guestGuard],
    },
    {
        path: 'auth/register',
        loadComponent: () =>
            import('./features/auth/register/register.component')
                .then(m => m.RegisterComponent),
        canActivate: [guestGuard],
    },
    {
        path: 'profile',
        loadComponent: () =>
            import('./features/profile/profile/profile.component')
                .then(m => m.ProfileComponent),
        canActivate: [authGuard],
    },
    {
        path: 'profile/watchlist',
        loadComponent: () =>
            import('./features/profile/watchlist/watchlist.component')
                .then(m => m.WatchlistComponent),
        canActivate: [authGuard],
    },
    { path: '**', redirectTo: '' },
];