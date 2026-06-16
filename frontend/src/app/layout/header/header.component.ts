import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationDto, NotificationType } from '../../core/models/notification.model';
import { TimeAgoPipe } from '../../shared/pipes/time-ago.pipe';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatBadgeModule,
    MatMenuModule,
    MatDividerModule,
    TranslatePipe,
    TimeAgoPipe,
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {
  protected auth = inject(AuthService);
  protected theme = inject(ThemeService);
  protected notifications = inject(NotificationService);
  private router = inject(Router);

  protected notifIcon(type: NotificationType): string {
    const icons: Record<NotificationType, string> = {
      BidOutbid: 'trending_up',
      AuctionWon: 'emoji_events',
      AuctionEndedSeller: 'gavel',
      ReserveMet: 'check_circle',
      WatchedAuctionEnding: 'timer',
      BuyItNowPurchased: 'shopping_cart',
    };
    return icons[type] ?? 'notifications';
  }

  protected notifIconClass(type: NotificationType): string {
    const classes: Record<NotificationType, string> = {
      BidOutbid: 'text-amber-500',
      AuctionWon: 'text-emerald-500',
      AuctionEndedSeller: 'text-slate-400',
      ReserveMet: 'text-emerald-500',
      WatchedAuctionEnding: 'text-amber-400',
      BuyItNowPurchased: 'text-emerald-500',
    };
    return classes[type] ?? 'text-slate-400';
  }

  protected onNotificationClick(notification: NotificationDto): void {
    if (!notification.isRead) {
      this.notifications.markAsRead(notification.id);
    }
    if (notification.payload) {
      this.router.navigate(['/auctions', notification.payload]);
    }
  }

  logout(): void {
    this.auth.logout();
  }
}
