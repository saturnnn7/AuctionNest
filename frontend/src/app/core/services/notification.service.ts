import { Injectable, inject, signal, computed, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../models/notification.model';
import { PagedResponse } from '../models/auction.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  private _notifications = signal<NotificationDto[]>([]);
  private _unreadCount = signal(0);

  readonly notifications = this._notifications.asReadonly();
  readonly unreadCount = this._unreadCount.asReadonly();
  readonly hasUnread = computed(() => this._unreadCount() > 0);

  constructor() {
    // Load unread count + recent notifications on auth; clear on logout
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.loadUnreadCount();
        this.loadRecent();
      } else {
        this._unreadCount.set(0);
        this._notifications.set([]);
      }
    }, { allowSignalWrites: true });
  }

  getNotifications(page = 1, pageSize = 5): Observable<PagedResponse<NotificationDto>> {
    return this.http.get<PagedResponse<NotificationDto>>(
      `${environment.apiUrl}/api/notifications`,
      { params: { page, pageSize } }
    );
  }

  loadUnreadCount(): void {
    this.http.get<number>(`${environment.apiUrl}/api/notifications/unread-count`)
      .subscribe(count => this._unreadCount.set(count));
  }

  loadRecent(): void {
    this.getNotifications(1, 5).subscribe(res => {
      this._notifications.set(res.items);
    });
  }

  markAsRead(id: string): void {
    this.http.patch<void>(`${environment.apiUrl}/api/notifications/${id}/read`, {}).subscribe(() => {
      this._notifications.update(list =>
        list.map(n => n.id === id ? { ...n, isRead: true } : n)
      );
      this._unreadCount.update(c => Math.max(0, c - 1));
    });
  }

  markAllRead(): void {
    this.http.patch<void>(`${environment.apiUrl}/api/notifications/read-all`, {}).subscribe(() => {
      this._unreadCount.set(0);
      this._notifications.update(list => list.map(n => ({ ...n, isRead: true })));
    });
  }

  addRealtime(notification: NotificationDto): void {
    this._notifications.update(list => [notification, ...list].slice(0, 5));
    this._unreadCount.update(c => c + 1);
  }
}
