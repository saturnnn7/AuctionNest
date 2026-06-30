import { Injectable, signal, computed } from '@angular/core';
import { AppNotification, NotificationType } from '@core/models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private _items = signal<AppNotification[]>([]);

  readonly notifications = this._items.asReadonly();
  readonly unreadCount   = computed(() => this._items().filter(n => !n.isRead).length);

  // Called by SignalR hub in Step 6
  add(data: { type: NotificationType; message: string; auctionId: string }): void {
    const item: AppNotification = {
      ...data,
      id: crypto.randomUUID(),
      isRead: false,
      createdAt: new Date().toISOString(),
    };
    // Cap at 50 notifications to avoid memory growth
    this._items.update(list => [item, ...list].slice(0, 50));
  }

  markAllRead(): void {
    this._items.update(list => list.map(n => ({ ...n, isRead: true })));
  }

  clear(): void {
    this._items.set([]);
  }
}
