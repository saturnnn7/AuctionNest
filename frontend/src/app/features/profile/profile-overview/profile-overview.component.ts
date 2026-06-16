import { Component, inject, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { AuctionDto } from '../../../core/models/auction.model';
import { AuthService } from '../../../core/services/auth.service';
import { UserService } from '../../../core/services/user.service';
import { AuctionService } from '../../../core/services/auction.service';
import { AuctionCardComponent } from '../../auctions/auction-card/auction-card.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-profile-overview',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatTabsModule,
    MatCardModule,
    TranslatePipe,
    AuctionCardComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
  ],
  templateUrl: './profile-overview.component.html',
})
export class ProfileOverviewComponent {
  private auth = inject(AuthService);
  private userService = inject(UserService);
  private auctionService = inject(AuctionService);
  private snackbar = inject(MatSnackBar);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly user = this.auth.currentUser;
  readonly profileEmail = signal<string | null>(null);

  // Inline display name edit
  readonly isEditingName = signal(false);
  readonly nameSaving = signal(false);
  readonly nameEditControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(50)],
  });

  // My auctions
  readonly myAuctions = signal<AuctionDto[]>([]);
  readonly loadingAuctions = signal(true);
  readonly activeAuctions = computed(() =>
    this.myAuctions().filter(a => a.status === 'Active' || a.status === 'Extending')
  );

  constructor() {
    // Fetch full profile to get email
    this.userService.getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: p => this.profileEmail.set(p.email), error: () => {} });

    // Load my auctions — API has no sellerId filter, filter client-side
    this.auctionService.getAuctions({ pageSize: 100, sortBy: 'endsAt', sortDescending: true })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          const uid = this.auth.currentUser()?.userId;
          this.myAuctions.set(uid ? res.items.filter(a => a.sellerId === uid) : res.items);
          this.loadingAuctions.set(false);
        },
        error: () => this.loadingAuctions.set(false),
      });
  }

  startEditName(): void {
    this.nameEditControl.setValue(this.user()?.displayName ?? '');
    this.isEditingName.set(true);
  }

  cancelEditName(): void {
    this.isEditingName.set(false);
    this.nameEditControl.reset('');
  }

  async saveName(): Promise<void> {
    if (this.nameEditControl.invalid || this.nameSaving()) return;
    this.nameSaving.set(true);
    try {
      await firstValueFrom(this.userService.updateDisplayName(this.nameEditControl.value));
      this.auth.updateDisplayName(this.nameEditControl.value);
      this.snackbar.open(
        this.translate.instant('PROFILE.NAME_UPDATED'),
        this.translate.instant('COMMON.CLOSE'),
        { duration: 3000, panelClass: 'snackbar-success' }
      );
      this.isEditingName.set(false);
    } catch {
      this.snackbar.open(
        this.translate.instant('ERRORS.GENERIC'),
        this.translate.instant('COMMON.CLOSE'),
        { duration: 4000 }
      );
    } finally {
      this.nameSaving.set(false);
    }
  }
}
