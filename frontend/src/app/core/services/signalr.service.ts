import { Injectable, inject, effect } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';
import { NotificationDto } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);

  private connection: HubConnection | null = null;

  constructor() {
    // Auto-connect/disconnect whenever the access token changes
    effect(() => {
      const token = this.authService.accessToken();
      if (token) {
        void this.startGlobalConnection();
      } else {
        void this.stopGlobalConnection();
      }
    }, { allowSignalWrites: true });
  }

  async startGlobalConnection(): Promise<void> {
    if (this.connection) return; // already connected (e.g. on silent refresh)

    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/auction`, {
        accessTokenFactory: () => this.authService.accessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('NewNotification', (notification: NotificationDto) => {
      this.notificationService.addRealtime(notification);
    });

    try {
      await this.connection.start();
    } catch {
      this.connection = null;
    }
  }

  async stopGlobalConnection(): Promise<void> {
    if (!this.connection) return;
    try {
      await this.connection.stop();
    } finally {
      this.connection = null;
    }
  }

  createAuctionConnection(): HubConnection {
    return new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/auction`, {
        accessTokenFactory: () => this.authService.accessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
  }
}
