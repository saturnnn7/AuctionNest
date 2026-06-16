import { Component, DestroyRef, inject, signal, computed } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { switchMap, finalize } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { trigger, transition, style, animate } from '@angular/animations';
import { HubConnection } from '@microsoft/signalr';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { AuctionDetailDto, ApiBidDto, BidDto } from '../../../core/models/auction.model';
import {
  BidPlacedEvent,
  AuctionExtendedEvent,
  AuctionEndedEvent,
  AuctionCancelledEvent,
} from '../../../core/models/signalr-events.model';
import { AuctionService } from '../../../core/services/auction.service';
import { AuthService } from '../../../core/services/auth.service';
import { WatchlistService } from '../../../core/services/watchlist.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { CountdownTimerComponent } from '../../../shared/components/countdown-timer/countdown-timer.component';
import { AuctionStatusBadgeComponent } from '../../../shared/components/auction-status-badge/auction-status-badge.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { TimeAgoPipe } from '../../../shared/pipes/time-ago.pipe';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../shared/components/confirm-dialog/confirm-dialog.component';

interface WinnerBanner {
  isWinner: boolean;
  winnerDisplayName: string | null;
  amount: number | null;
}

@Component({
  selector: 'app-auction-detail',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDividerModule,
    TranslatePipe,
    CountdownTimerComponent,
    AuctionStatusBadgeComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    TimeAgoPipe,
  ],
  templateUrl: './auction-detail.component.html',
  styleUrl: './auction-detail.component.scss',
  animations: [
    trigger('slideFadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(-10px)' }),
        animate('200ms ease-out', style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
    ]),
  ],
})
export class AuctionDetailComponent {
  private route = inject(ActivatedRoute);
  private auctionService = inject(AuctionService);
  protected readonly auth = inject(AuthService);
  private watchlist = inject(WatchlistService);
  private signalrService = inject(SignalrService);
  private router = inject(Router);
  private snackbar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  // ── State signals ─────────────────────────────────────────────
  readonly auction = signal<AuctionDetailDto | null>(null);
  readonly loading = signal(true);
  readonly bids = signal<BidDto[]>([]);
  readonly descriptionExpanded = signal(false);
  readonly bidLoading = signal(false);
  readonly buyItNowLoading = signal(false);
  readonly winnerBanner = signal<WinnerBanner | null>(null);
  readonly submitted = signal(false);
  readonly showAllBids = signal(false);

  // ── Computed ──────────────────────────────────────────────────
  readonly isActive = computed(() => {
    const s = this.auction()?.status;
    return s === 'Active' || s === 'Extending';
  });

  readonly isCancelled = computed(() => this.auction()?.status === 'Cancelled');

  readonly isWatched = computed(() =>
    this.watchlist.watchedIds().has(this.auction()?.id ?? '')
  );

  readonly minBidAmount = computed(() => {
    const a = this.auction();
    return a ? a.currentPrice + a.minBidIncrement : 0;
  });

  readonly minBidFormatted = computed(() =>
    this.minBidAmount().toLocaleString('en-US', { style: 'currency', currency: 'USD' })
  );

  readonly descriptionTruncated = computed(
    () => (this.auction()?.description ?? '').length > 300
  );

  readonly displayedDescription = computed(() => {
    const desc = this.auction()?.description ?? '';
    if (!this.descriptionTruncated() || this.descriptionExpanded()) return desc;
    return `${desc.slice(0, 300)}…`;
  });

  // ── Bid form ──────────────────────────────────────────────────
  private readonly bidAmountValidator = (control: AbstractControl): ValidationErrors | null => {
    const val = Number(control.value);
    if (!control.value || isNaN(val)) return null;
    const min = this.minBidAmount();
    return val >= min ? null : { bidTooLow: { min } };
  };

  readonly bidForm = new FormGroup({
    amount: new FormControl<number | null>(null, [Validators.required, this.bidAmountValidator]),
  });

  readonly hiddenBidCount = computed(() =>
    Math.max(0, this.bids().length - this.INITIAL_BID_LIMIT)
  );

  readonly visibleBids = computed(() =>
    this.showAllBids() ? this.bids() : this.bids().slice(0, this.INITIAL_BID_LIMIT)
  );

  // ── Local SignalR connection ──────────────────────────────────
  private localConnection: HubConnection | null = null;
  private auctionId: string | null = null;
  private bidderAliasMap = new Map<string, string>();
  private bidderCounter = 0;
  private readonly INITIAL_BID_LIMIT = 5;

  constructor() {
    this.destroyRef.onDestroy(() => void this.leaveAndStopAuctionSignalR());

    this.route.paramMap.pipe(
      switchMap(params => {
        this.loading.set(true);
        this.auction.set(null);
        this.winnerBanner.set(null);
        this.submitted.set(false);
        this.showAllBids.set(false);
        this.bidderAliasMap.clear();
        this.bidderCounter = 0;
        return this.auctionService.getById(params.get('id') ?? '').pipe(
          finalize(() => this.loading.set(false))
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: auction => {
        this.auction.set(auction);
        this.auctionId = auction.id;
        this.bids.set([...(auction.recentBids ?? [])].reverse().map(b => this.mapApiBid(b)));
        this.auction.update(a => a ? { ...a, bidCount: auction.totalBids } : a);
        this.bidForm.controls.amount.setValue(this.minBidAmount());
        void this.startAuctionSignalR(auction.id);
      },
      error: () => {},
    });
  }

  // ── SignalR lifecycle ─────────────────────────────────────────
  private async startAuctionSignalR(id: string): Promise<void> {
    await this.leaveAndStopAuctionSignalR();

    this.localConnection = this.signalrService.createAuctionConnection();
    this.registerAuctionHandlers(id);

    try {
      await this.localConnection.start();
      await this.localConnection.invoke('JoinAuction', id);
    } catch {
      // Non-fatal: real-time won't work but page is still usable
    }
  }

  private async leaveAndStopAuctionSignalR(): Promise<void> {
    if (!this.localConnection) return;
    try {
      if (this.auctionId) {
        await this.localConnection.invoke('LeaveAuction', this.auctionId);
      }
      await this.localConnection.stop();
    } catch {
      // Ignore errors on cleanup
    } finally {
      this.localConnection = null;
    }
  }

  private registerAuctionHandlers(id: string): void {
    const conn = this.localConnection!;

    conn.on('BidPlaced', (event: BidPlacedEvent) => {
      if (event.auctionId !== id) return;

      const currentUserId = this.auth.currentUser()?.userId;
      const wasUserWinning = currentUserId
        ? this.bids().some(b => b.isWinning && b.bidderId === currentUserId)
        : false;

      // Dedup: if the current user placed this bid optimistically via HTTP, it's already in the
      // list with a real server ID. The SignalR event doesn't carry bidId, so we can't match by
      // id — instead check bidderId + amount. The same user can never bid the same amount twice
      // consecutively (minBidIncrement enforces strictly higher bids each round).
      const isOptimisticDuplicate = event.bidderId === currentUserId &&
        this.bids().some(b => b.bidderId === event.bidderId && b.amount === event.amount);

      if (!isOptimisticDuplicate) {
        const currentUser = this.auth.currentUser();
        const bid: BidDto = {
          id: event.bidId ?? crypto.randomUUID(),
          auctionId: event.auctionId,
          bidderId: event.bidderId,
          bidderDisplayName: this.resolveBidderAlias(event.bidderId, currentUser?.userId, currentUser?.displayName),
          amount: event.amount,
          isWinning: true,
          createdAt: event.occurredAt,
        };
        this.addBidToTimeline(bid);
      }

      this.updateCurrentPrice(event.newCurrentPrice);

      if (wasUserWinning && event.bidderId !== currentUserId) {
        this.snackbar.open(
          this.translate.instant('AUCTION.OUTBID'),
          this.translate.instant('COMMON.CLOSE'),
          { duration: 4000, panelClass: 'snackbar-warn' }
        );
      }
    });

    conn.on('AuctionExtended', (event: AuctionExtendedEvent) => {
      if (event.auctionId !== id) return;
      this.extendAuction(event.newEndsAt);
      this.snackbar.open(
        this.translate.instant('AUCTION.EXTENDED'),
        this.translate.instant('COMMON.CLOSE'),
        { duration: 4000 }
      );
    });

    conn.on('AuctionEnded', (event: AuctionEndedEvent) => {
      if (event.auctionId !== id) return;
      this.endAuction(event.winnerId);
    });

    conn.on('AuctionCancelled', (event: AuctionCancelledEvent) => {
      if (event.auctionId !== id) return;
      this.auction.update(a => (a ? { ...a, status: 'Cancelled', isBuyItNowAvailable: false } : a));
    });
  }

  // ── UI handlers ───────────────────────────────────────────────
  onImageError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }

  toggleDescription(): void {
    this.descriptionExpanded.update(v => !v);
  }

  toggleBidHistory(): void {
    this.showAllBids.update(v => !v);
  }

  toggleWatchlist(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return;
    }
    const id = this.auction()?.id;
    if (id) this.watchlist.toggle(id);
  }

  async placeBid(): Promise<void> {
    this.submitted.set(true);
    if (this.bidForm.invalid || this.bidLoading()) return;
    const amount = Number(this.bidForm.controls.amount.value);
    if (isNaN(amount)) return;
    const auction = this.auction();
    if (!auction) return;

    this.bidLoading.set(true);
    try {
      const bid = await firstValueFrom(
        this.auctionService.placeBid(auction.id, amount, crypto.randomUUID())
      );
      this.addBidToTimeline(bid);
      this.snackbar.open(
        this.translate.instant('AUCTION.BID_PLACED'),
        this.translate.instant('COMMON.CLOSE'),
        { duration: 3000, panelClass: 'snackbar-success' }
      );
      this.submitted.set(false);
      this.bidForm.reset();
      this.bidForm.markAsPristine();
      this.bidForm.markAsUntouched();
    } catch (err: unknown) {
      const message =
        (err as { error?: { detail?: string } })?.error?.detail ??
        this.translate.instant('ERRORS.GENERIC');
      this.snackbar.open(message, this.translate.instant('COMMON.CLOSE'), { duration: 5000 });
    } finally {
      this.bidLoading.set(false);
    }
  }

  async confirmBuyItNow(): Promise<void> {
    const auction = this.auction();
    if (!auction?.buyItNowPrice || !this.isActive()) return;
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return;
    }

    const price = auction.buyItNowPrice.toLocaleString('en-US', {
      style: 'currency',
      currency: 'USD',
    });
    const dialogData: ConfirmDialogData = {
      title: this.translate.instant('AUCTION.BUY_NOW_CONFIRM_TITLE'),
      message: this.translate.instant('AUCTION.BUY_NOW_CONFIRM_BODY', { price }),
      confirmLabel: this.translate.instant('AUCTION.BUY_NOW_CONFIRM_BTN'),
    };

    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDialogComponent, { data: dialogData, width: '380px' })
        .afterClosed()
    );
    if (!confirmed) return;

    this.buyItNowLoading.set(true);
    try {
      await firstValueFrom(this.auctionService.buyItNow(auction.id));
      this.snackbar.open(
        this.translate.instant('AUCTION.BUY_NOW_SUCCESS'),
        this.translate.instant('COMMON.CLOSE'),
        { duration: 5000, panelClass: 'snackbar-success' }
      );
      const updated = await firstValueFrom(this.auctionService.getById(auction.id));
      this.auction.set(updated);
      this.bids.set([...(updated.recentBids ?? [])].reverse().map(b => this.mapApiBid(b)));
      this.auction.update(a => a ? { ...a, bidCount: updated.totalBids } : a);
    } catch (err: unknown) {
      const message =
        (err as { error?: { detail?: string } })?.error?.detail ??
        this.translate.instant('ERRORS.GENERIC');
      this.snackbar.open(message, this.translate.instant('COMMON.CLOSE'), { duration: 5000 });
    } finally {
      this.buyItNowLoading.set(false);
    }
  }

  // ── SignalR public hooks (also used by placeBid / confirmBuyItNow) ──

  private mapApiBid(b: ApiBidDto): BidDto {
    const currentUser = this.auth.currentUser();
    return {
      id: b.id,
      auctionId: b.auctionId,
      bidderId: b.bidderId,
      bidderDisplayName: this.resolveBidderAlias(b.bidderId, currentUser?.userId, currentUser?.displayName),
      amount: b.amount,
      isWinning: b.isWinning,
      createdAt: b.placedAt,
    };
  }

  private resolveBidderAlias(
    bidderId: string,
    currentUserId: string | undefined,
    currentDisplayName: string | undefined,
  ): string {
    if (bidderId === currentUserId) {
      return currentDisplayName ?? 'You';
    }
    if (!this.bidderAliasMap.has(bidderId)) {
      this.bidderCounter++;
      this.bidderAliasMap.set(bidderId, `Bidder ${this.bidderCounter}`);
    }
    return this.bidderAliasMap.get(bidderId)!;
  }

  addBidToTimeline(bid: BidDto): void {
    if (this.bids().some(b => b.id === bid.id)) return;
    this.bids.update(prev => [
      { ...bid, isWinning: true },
      ...prev.map(b => ({ ...b, isWinning: false })),
    ]);
    this.auction.update(a => (a ? { ...a, bidCount: a.bidCount + 1 } : a));
  }

  updateCurrentPrice(amount: number): void {
    this.auction.update(a => (a ? { ...a, currentPrice: amount } : a));
    this.bidForm.controls.amount.setValue(this.minBidAmount());
  }

  extendAuction(newEndsAt: string): void {
    this.auction.update(a => (a ? { ...a, endsAt: newEndsAt, status: 'Extending' } : a));
  }

  endAuction(winnerId: string | null): void {
    this.auction.update(a => (a ? { ...a, status: 'Ended', isBuyItNowAvailable: false } : a));

    const currentUserId = this.auth.currentUser()?.userId;
    const winningBid = winnerId
      ? (this.bids().find(b => b.bidderId === winnerId) ?? this.bids().find(b => b.isWinning))
      : null;

    this.winnerBanner.set({
      isWinner: winnerId !== null && winnerId === currentUserId,
      winnerDisplayName: winningBid?.bidderDisplayName ?? null,
      amount: winningBid?.amount ?? null,
    });
  }
}
