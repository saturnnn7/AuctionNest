import { Component, Input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AuctionStatus } from '@core/models/auction.model';

@Component({
    selector: 'app-auction-status-badge',
    standalone: true,
    imports: [TranslatePipe],
    styles: [`
        :host { display: inline-flex; }
        .badge {
            display: inline-flex; align-items: center; gap: 5px;
            padding: 2px 9px; border-radius: 9999px;
            font-size: 11px; font-weight: 500; line-height: 1.6;
        }
        .badge::before {
            content: ''; width: 6px; height: 6px;
            border-radius: 50%; background: currentColor; flex-shrink: 0;
        }
        .badge-active    { color: #16A34A; background: #f0fdf4; }
        .badge-ended     { color: #64748B; background: #f8fafc; }
        .badge-cancelled { color: #DC2626; background: #fef2f2; }
        .badge-pending   { color: #D97706; background: #fffbeb; }

        :host-context(.dark-theme) .badge-active    { background: rgba(22,163,74,0.12); }
        :host-context(.dark-theme) .badge-ended     { background: rgba(100,116,139,0.15); }
        :host-context(.dark-theme) .badge-cancelled { background: rgba(220,38,38,0.12); }
        :host-context(.dark-theme) .badge-pending   { background: rgba(217,119,6,0.12); }
    `],
    template: `
        <span [class]="'badge badge-' + status.toLowerCase()">
            {{ 'AUCTION.STATUS.' + status.toUpperCase() | translate }}
        </span>
    `,
})
export class AuctionStatusBadgeComponent {
    @Input({ required: true }) status!: AuctionStatus;
}