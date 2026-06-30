import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
// Cross-layer: shared → core — use alias
import { AuctionSummaryDto } from '@core/models/auction.model';

// Intra-layer: within shared/ — use relative path
import { AuctionStatusBadgeComponent } from '../auction-status-badge/auction-status-badge.component';
import { CountdownTimerComponent } from '../countdown-timer/countdown-timer.component';

@Component({
  selector: 'app-auction-card',
  standalone: true,
  imports: [
    CurrencyPipe,
    TranslatePipe,
    AuctionStatusBadgeComponent,
    CountdownTimerComponent,
  ],
  styles: [`
    .card { cursor: pointer; transition: box-shadow 0.2s ease, transform 0.2s ease; }
    .card:hover { box-shadow: 0 4px 20px rgba(0,0,0,0.1); transform: translateY(-2px); }
    .img-placeholder {
      background: var(--color-border); display: flex;
      align-items: center; justify-content: center;
    }
    .heart-btn {
      background: none; border: none; padding: 4px; cursor: pointer;
      border-radius: 6px; display: flex; transition: background 0.15s;
    }
    .heart-btn:hover { background: var(--color-border); }
    .heart { font-size: 20px; transition: color 0.15s; }
    .heart-on  { color: #2563EB; }
    .heart-off { color: var(--color-muted); }
  `],
  template: `
    <article
      class="card rounded-lg overflow-hidden"
      style="border: 1px solid var(--color-border); background: var(--color-surface)"
      (click)="navigate()"
      (keydown.enter)="navigate()"
      tabindex="0"
      [attr.aria-label]="auction.title">

      <!-- 16:9 image -->
      <div class="relative overflow-hidden" style="aspect-ratio:16/9">
        @if (auction.imageUrl) {
          <img
            [src]="auction.imageUrl"
            [alt]="auction.title"
            class="w-full h-full object-cover"
            loading="lazy">
        } @else {
          <div class="img-placeholder w-full h-full">
            <span class="material-icons" style="font-size:40px; color:var(--color-border)">image</span>
          </div>
        }
      </div>

      <!-- Card body -->
      <div class="p-4">

        <!-- Category + Status -->
        <div class="flex items-center justify-between gap-2 mb-1">
          <span class="text-xs truncate" style="color:var(--color-muted)">
            {{ auction.categoryName }}
          </span>
          <app-auction-status-badge [status]="auction.status" />
        </div>

        <!-- Title -->
        <h3 class="text-sm font-medium mb-3 line-clamp-2" style="color:var(--color-text)">
          {{ auction.title }}
        </h3>

        <!-- Price + Timer -->
        <div class="grid grid-cols-2 gap-3 mb-3">
          <div>
            <div class="text-xs mb-0.5" style="color:var(--color-muted)">
              {{ 'AUCTION.DETAIL.CURRENT_PRICE' | translate }}
            </div>
            <div class="text-lg font-semibold" style="color:var(--color-text)">
              {{ auction.currentPrice | currency:'USD':'symbol':'1.2-2' }}
            </div>
          </div>
          <div>
            <div class="text-xs mb-0.5" style="color:var(--color-muted)">
              {{ 'AUCTION.DETAIL.TIME_LEFT' | translate }}
            </div>
            @if (auction.status === 'Active') {
              <app-countdown-timer [endTime]="auction.endTime" />
            } @else {
              <span class="text-sm" style="color:var(--color-muted)">—</span>
            }
          </div>
        </div>

        <!-- Bids count + Watchlist heart -->
        <div class="flex items-center justify-between">
          <span class="text-xs" style="color:var(--color-muted)">
            {{ auction.totalBids }} {{ 'AUCTION.DETAIL.BIDS' | translate }}
          </span>
          <button
            class="heart-btn"
            (click)="onWatchToggle($event)"
            [attr.aria-label]="(isWatched ? 'AUCTION.DETAIL.REMOVE_WATCHLIST' : 'AUCTION.DETAIL.ADD_WATCHLIST') | translate">
            <span class="material-icons heart"
                  [class.heart-on]="isWatched"
                  [class.heart-off]="!isWatched">
              {{ isWatched ? 'favorite' : 'favorite_border' }}
            </span>
          </button>
        </div>

      </div>
    </article>
  `,
})
export class AuctionCardComponent {
    @Input({ required: true }) auction!: AuctionSummaryDto;
    @Input() isWatched = false;
    @Output() watchToggle = new EventEmitter<string>();

    private router = inject(Router);

    navigate(): void {
        this.router.navigate(['/auctions', this.auction.id]);
    }

    onWatchToggle(event: Event): void {
        event.stopPropagation(); // prevent card click from triggering navigate()
        this.watchToggle.emit(this.auction.id);
    }
}