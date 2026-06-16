import { Injectable, inject, signal, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuctionDto } from '../models/auction.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class WatchlistService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  private _watchedIds = signal<Set<string>>(new Set());
  readonly watchedIds = this._watchedIds.asReadonly();

  constructor() {
    // Auto-load watchlist when user authenticates; clear on logout
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.loadWatchedIds();
      } else {
        this._watchedIds.set(new Set());
      }
    }, { allowSignalWrites: true });
  }

  isWatched(auctionId: string): boolean {
    return this._watchedIds().has(auctionId);
  }

  private loadWatchedIds(): void {
    this.http.get<AuctionDto[]>(`${environment.apiUrl}/api/watchlist`).subscribe({
      next: auctions => this._watchedIds.set(new Set(auctions.map(a => a.id))),
      error: () => { /* silently ignore — user might not have a watchlist yet */ }
    });
  }

  getWatchlist(): Observable<AuctionDto[]> {
    return this.http.get<AuctionDto[]>(`${environment.apiUrl}/api/watchlist`);
  }

  add(auctionId: string): void {
    this.http.post<void>(`${environment.apiUrl}/api/watchlist/${auctionId}`, {}).subscribe({
      next: () => this._watchedIds.update(s => new Set([...s, auctionId])),
    });
  }

  remove(auctionId: string): void {
    this.http.delete<void>(`${environment.apiUrl}/api/watchlist/${auctionId}`).subscribe({
      next: () => this._watchedIds.update(s => {
        const next = new Set(s);
        next.delete(auctionId);
        return next;
      }),
    });
  }

  toggle(auctionId: string): void {
    this.isWatched(auctionId) ? this.remove(auctionId) : this.add(auctionId);
  }
}
