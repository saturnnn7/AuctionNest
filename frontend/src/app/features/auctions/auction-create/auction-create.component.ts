import { Component, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, finalize } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { CreateAuctionRequest } from '../../../core/models/auction.model';
import { CategoryDto } from '../../../core/models/category.model';
import { AuctionService } from '../../../core/services/auction.service';
import { CategoryService } from '../../../core/services/category.service';

// ── Module-level helpers ─────────────────────────────────────────

function combineDateAndTime(date: Date, time: string): Date {
  const [h, m] = time.split(':').map(Number);
  const result = new Date(date);
  result.setHours(h || 0, m || 0, 0, 0);
  return result;
}

function auctionFormValidator(group: AbstractControl): ValidationErrors | null {
  const errors: ValidationErrors = {};

  const startPrice = Number(group.get('startPrice')?.value) || 0;
  const reservePrice = Number(group.get('reservePrice')?.value) || 0;
  const buyItNowPrice = Number(group.get('buyItNowPrice')?.value) || 0;
  const startsAtDate = group.get('startsAtDate')?.value as Date | null;
  const startsAtTime = (group.get('startsAtTime')?.value as string) || '12:00';
  const endsAtDate = group.get('endsAtDate')?.value as Date | null;
  const endsAtTime = (group.get('endsAtTime')?.value as string) || '12:00';

  if (reservePrice > 0 && startPrice > 0 && reservePrice <= startPrice) {
    errors['reserveTooLow'] = true;
  }
  if (buyItNowPrice > 0 && startPrice > 0 && buyItNowPrice <= startPrice) {
    errors['binTooLow'] = true;
  }
  if (startsAtDate && endsAtDate) {
    const starts = combineDateAndTime(startsAtDate, startsAtTime);
    const ends = combineDateAndTime(endsAtDate, endsAtTime);
    if (starts.getTime() < Date.now() + 60_000) errors['startsInPast'] = true;
    if (ends.getTime() - starts.getTime() < 3_600_000) errors['endsTooSoon'] = true;
  }

  return Object.keys(errors).length ? errors : null;
}

// ── Component ────────────────────────────────────────────────────

@Component({
  selector: 'app-auction-create',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule,
    MatDatepickerModule,
    MatNativeDateModule,
    TranslatePipe,
  ],
  templateUrl: './auction-create.component.html',
  styleUrl: './auction-create.component.scss',
})
export class AuctionCreateComponent {
  private auctionService = inject(AuctionService);
  private categoryService = inject(CategoryService);
  private router = inject(Router);
  private snackbar = inject(MatSnackBar);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly submitted = signal(false);
  readonly categories = signal<CategoryDto[]>([]);
  readonly imagePreviewUrl = signal<string | null>(null);
  readonly minDate = new Date();

  readonly form = new FormGroup(
    {
      title: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(3), Validators.maxLength(100)],
      }),
      description: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(10), Validators.maxLength(5000)],
      }),
      categoryId: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
      imageUrl: new FormControl('', { nonNullable: true }),
      startPrice: new FormControl<number | null>(null, [
        Validators.required,
        Validators.min(0.01),
      ]),
      minBidIncrement: new FormControl<number | null>(null, [
        Validators.required,
        Validators.min(0.01),
      ]),
      reservePrice: new FormControl<number | null>(null),
      buyItNowPrice: new FormControl<number | null>(null),
      startsAtDate: new FormControl<Date | null>(null, [Validators.required]),
      startsAtTime: new FormControl('12:00', { nonNullable: true }),
      endsAtDate: new FormControl<Date | null>(null, [Validators.required]),
      endsAtTime: new FormControl('12:00', { nonNullable: true }),
    },
    { validators: auctionFormValidator }
  );

  constructor() {
    this.categoryService.getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(cats => this.categories.set(cats));

    // Image preview — debounced so we don't fire on every keystroke
    this.form.controls.imageUrl.valueChanges.pipe(
      debounceTime(400),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(url => {
      this.imagePreviewUrl.set(url && url.startsWith('http') ? url : null);
    });
  }

  submit(): void {
    this.submitted.set(true);
    this.form.markAllAsTouched();

    if (this.form.invalid || this.loading()) {
      setTimeout(() => {
        const el = document.querySelector('mat-form-field.ng-invalid');
        el?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      });
      return;
    }

    const v = this.form.getRawValue();
    const starts = combineDateAndTime(v.startsAtDate!, v.startsAtTime);
    const ends = combineDateAndTime(v.endsAtDate!, v.endsAtTime);

    const request: CreateAuctionRequest = {
      title: v.title,
      description: v.description,
      categoryId: v.categoryId,
      imageUrl: v.imageUrl || undefined,
      startPrice: Number(v.startPrice),
      minBidIncrement: Number(v.minBidIncrement),
      reservePrice: v.reservePrice ? Number(v.reservePrice) : undefined,
      buyItNowPrice: v.buyItNowPrice ? Number(v.buyItNowPrice) : undefined,
      startsAt: starts.toISOString(),
      endsAt: ends.toISOString(),
    };

    this.loading.set(true);
    this.auctionService.create(request).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.loading.set(false)),
    ).subscribe({
      next: (auction) => {
        const dateStr = starts.toLocaleDateString('en-US', {
          month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
        });
        this.snackbar.open(
          this.translate.instant('CREATE_AUCTION.SUCCESS_MSG', { date: dateStr }),
          this.translate.instant('COMMON.CLOSE'),
          { duration: 5000, panelClass: 'snackbar-success' }
        );
        void this.router.navigate(['/auctions', auction.id]);
      },
      error: (err: unknown) => {
        const message =
          (err as { error?: { detail?: string } })?.error?.detail ??
          this.translate.instant('ERRORS.GENERIC');
        this.snackbar.open(message, this.translate.instant('COMMON.CLOSE'), { duration: 5000 });
      },
    });
  }
}
