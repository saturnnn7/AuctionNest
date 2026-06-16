import { Component, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { trigger, transition, style, animate } from '@angular/animations';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';

import { AuctionDto } from '../../../core/models/auction.model';
import { WatchlistService } from '../../../core/services/watchlist.service';
import { AuctionCardComponent } from '../../auctions/auction-card/auction-card.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    TranslatePipe,
    AuctionCardComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
  ],
  animations: [
    trigger('slideOut', [
      transition(':leave', [
        style({ opacity: 1, transform: 'scale(1)' }),
        animate('180ms ease-in', style({ opacity: 0, transform: 'scale(0.93)' })),
      ]),
    ]),
  ],
  templateUrl: './watchlist.component.html',
})
export class WatchlistComponent {
  private watchlistService = inject(WatchlistService);
  private destroyRef = inject(DestroyRef);

  readonly auctions = signal<AuctionDto[]>([]);
  readonly loading = signal(true);

  constructor() {
    this.watchlistService.getWatchlist()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: items => {
          this.auctions.set(items);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  onToggle(auctionId: string): void {
    // AuctionCardComponent already called WatchlistService.toggle() — just remove from local list
    this.auctions.update(list => list.filter(a => a.id !== auctionId));
  }
}
