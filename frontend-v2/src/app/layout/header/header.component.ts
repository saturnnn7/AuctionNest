import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '@core/services/auth.service';
import { ThemeService } from '@core/services/theme.service';
import { NotificationService } from '@core/services/notification.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule,
    TranslatePipe,
  ],
  styles: [`
    .header-inner {
      max-width: 1280px; margin: 0 auto;
      padding: 0 1rem; height: 56px;
      display: flex; align-items: center; gap: 8px;
    }

    .logo {
      font-size: 1.125rem; font-weight: 700;
      text-decoration: none; margin-right: 8px;
      display: flex; align-items: center;
    }
    .logo-accent { color: #2563EB; }
    .logo-text   { color: var(--color-text); }

    .nav-link {
      padding: 6px 12px; border-radius: 6px;
      font-size: 14px; font-weight: 500;
      text-decoration: none; color: var(--color-muted);
      transition: color 0.15s, background 0.15s;
    }
    .nav-link:hover        { color: var(--color-text); background: var(--color-surface); }
    .nav-link.active-link  { color: var(--color-accent); }

    .spacer { flex: 1; }

    /* CCD: Avatar circle shows first letter of display name */
    .avatar {
      width: 32px; height: 32px; border-radius: 50%;
      background: #2563EB; color: white;
      font-size: 13px; font-weight: 600;
      display: flex; align-items: center; justify-content: center;
    }

    .user-info {
      padding: 10px 16px 8px;
      border-bottom: 1px solid var(--color-border);
      pointer-events: none;
    }
    .user-name    { font-size: 14px; font-weight: 500; color: var(--color-text); }
    .user-handle  { font-size: 12px; color: var(--color-muted); }

    /* CCD: Notification items mark visual difference for unread */
    .notif-header {
      padding: 10px 16px 8px;
      display: flex; align-items: center; justify-content: space-between;
      border-bottom: 1px solid var(--color-border);
      pointer-events: none;
    }
    .notif-header-title { font-size: 14px; font-weight: 500; color: var(--color-text); }
    .notif-mark-read {
      font-size: 12px; color: #2563EB; background: none;
      border: none; cursor: pointer; padding: 0; pointer-events: all;
    }
    .notif-empty {
      padding: 24px 16px; text-align: center;
      font-size: 13px; color: var(--color-muted);
    }
    .notif-unread { background: rgba(37, 99, 235, 0.05) !important; }

    /* CCD: Theme toggle is subtle — doesn't compete with main CTA */
    .theme-btn { opacity: 0.7; }
    .theme-btn:hover { opacity: 1; }
  `],
  template: `
    <!-- CCD: Sticky header keeps primary CTA always in viewport -->
    <header class="sticky top-0 z-50"
            style="background: var(--color-bg);
                   border-bottom: 1px solid var(--color-border);
                   backdrop-filter: blur(8px);">
      <div class="header-inner">

        <!-- CCD: Logo as trust anchor — leftmost, instant brand recognition -->
        <a routerLink="/" class="logo">
          <span class="logo-accent">Auction</span>
          <span class="logo-text">Nest</span>
        </a>

        <!-- Primary nav -->
        <nav>
          <a routerLink="/"
             routerLinkActive="active-link"
             [routerLinkActiveOptions]="{ exact: true }"
             class="nav-link">
            {{ 'NAV.AUCTIONS' | translate }}
          </a>
        </nav>

        <div class="spacer"></div>

        <!-- Actions -->
        <div style="display: flex; align-items: center; gap: 4px;">

          <!-- CCD: Theme toggle is subtle — doesn't steal focus from CTA -->
          <button mat-icon-button class="theme-btn"
                  (click)="themeService.toggle()"
                  [attr.aria-label]="themeService.isDark() ? 'Switch to light mode' : 'Switch to dark mode'">
            <mat-icon>{{ themeService.isDark() ? 'light_mode' : 'dark_mode' }}</mat-icon>
          </button>

          @if (authService.isAuthenticated()) {

            <!-- CCD: Bell is placed before CTA to create urgency before action -->
            <button mat-icon-button
                    [matMenuTriggerFor]="notifMenu"
                    [matBadge]="notifService.unreadCount()"
                    [matBadgeHidden]="notifService.unreadCount() === 0"
                    matBadgeColor="warn"
                    matBadgeSize="small"
                    aria-label="Notifications">
              <mat-icon>notifications</mat-icon>
            </button>

            <mat-menu #notifMenu="matMenu" xPosition="before" panelClass="notifications-menu">
              <div class="notif-header" (click)="$event.stopPropagation()">
                <span class="notif-header-title">{{ 'NOTIFICATION.TITLE' | translate }}</span>
                @if (notifService.unreadCount() > 0) {
                  <button class="notif-mark-read" (click)="notifService.markAllRead()">
                    Mark all read
                  </button>
                }
              </div>
              @if (notifService.notifications().length === 0) {
                <div class="notif-empty">{{ 'NOTIFICATION.EMPTY' | translate }}</div>
              } @else {
                @for (n of notifService.notifications(); track n.id) {
                  <button mat-menu-item [class.notif-unread]="!n.isRead"
                          [routerLink]="['/auctions', n.auctionId]">
                    <span style="font-size: 13px; white-space: normal; line-height: 1.4">
                      {{ n.message }}
                    </span>
                  </button>
                }
              }
            </mat-menu>

            <!-- CCD: Primary CTA for sellers — create revenue event -->
            <a routerLink="/auctions/create" mat-flat-button color="primary"
               style="margin-left: 4px; font-size: 13px;">
              <mat-icon style="font-size: 18px; width: 18px; height: 18px; margin-right: 2px">
                add
              </mat-icon>
              {{ 'NAV.CREATE' | translate }}
            </a>

            <!-- User avatar menu -->
            <button mat-icon-button [matMenuTriggerFor]="userMenu"
                    style="margin-left: 2px;"
                    [attr.aria-label]="'User menu for ' + authService.user()?.displayName">
              <span class="avatar">{{ userInitial }}</span>
            </button>

            <mat-menu #userMenu="matMenu" xPosition="before">
              <div class="user-info">
                <div class="user-name">{{ authService.user()?.displayName }}</div>
                <div class="user-handle">@{{ authService.user()?.username }}</div>
              </div>
              <button mat-menu-item routerLink="/profile">
                <mat-icon>person</mat-icon>
                <span>{{ 'NAV.PROFILE' | translate }}</span>
              </button>
              <button mat-menu-item routerLink="/profile/watchlist">
                <mat-icon>favorite</mat-icon>
                <span>{{ 'NAV.WATCHLIST' | translate }}</span>
              </button>
              <mat-divider />
              <button mat-menu-item (click)="logout()">
                <mat-icon>logout</mat-icon>
                <span>{{ 'NAV.LOGOUT' | translate }}</span>
              </button>
            </mat-menu>

          } @else {

            <!-- CCD: Two-button pattern — ghost Sign In, primary Sign Up -->
            <!-- Sign In is low-friction (ghost), Sign Up is the conversion goal -->
            <a routerLink="/auth/login" mat-button style="font-size: 13px;">
              {{ 'NAV.LOGIN' | translate }}
            </a>
            <a routerLink="/auth/register" mat-flat-button color="primary"
               style="font-size: 13px; margin-left: 4px;">
              {{ 'NAV.REGISTER' | translate }}
            </a>

          }
        </div>

      </div>
    </header>
  `,
})
export class HeaderComponent {
  protected authService    = inject(AuthService);
  protected themeService   = inject(ThemeService);
  protected notifService   = inject(NotificationService);
  private   router         = inject(Router);

  // CCD: First letter avatar — personal, builds user connection
  get userInitial(): string {
    return this.authService.user()?.displayName?.[0]?.toUpperCase() ?? '?';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
