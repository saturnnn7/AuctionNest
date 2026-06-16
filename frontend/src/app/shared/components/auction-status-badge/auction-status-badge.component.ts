import { Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { AuctionStatus } from '../../../core/models/auction.model';

@Component({
  selector: 'app-auction-status-badge',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <span [class]="badgeClass()">
      {{ 'AUCTION.STATUS.' + status().toUpperCase() | translate }}
    </span>
  `,
})
export class AuctionStatusBadgeComponent {
  status = input.required<AuctionStatus>();

  badgeClass = computed(() => {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium';
    const colorMap: Record<AuctionStatus, string> = {
      Draft:     'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300',
      Active:    'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
      Extending: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400',
      Ended:     'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400',
      Cancelled: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
    };
    return `${base} ${colorMap[this.status()]}`;
  });
}
