import { Component, inject, input, output, computed } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { AuctionDto } from '../../../core/models/auction.model';
import { AuthService } from '../../../core/services/auth.service';
import { WatchlistService } from '../../../core/services/watchlist.service';
import { CountdownTimerComponent } from '../../../shared/components/countdown-timer/countdown-timer.component';
import { AuctionStatusBadgeComponent } from '../../../shared/components/auction-status-badge/auction-status-badge.component';

@Component({
  selector: 'app-auction-card',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    TranslatePipe,
    CountdownTimerComponent,
    AuctionStatusBadgeComponent,
  ],
  templateUrl: './auction-card.component.html',
})
export class AuctionCardComponent {
  private auth = inject(AuthService);
  private watchlist = inject(WatchlistService);
  private router = inject(Router);

  auction = input.required<AuctionDto>();
  watchlistToggled = output<string>();

  isWatched = computed(() => this.watchlist.watchedIds().has(this.auction().id));
  isActive = computed(() => ['Active', 'Extending'].includes(this.auction().status));

  onWatchlistClick(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.watchlist.toggle(this.auction().id);
    this.watchlistToggled.emit(this.auction().id);
  }

  onImageError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }
}
