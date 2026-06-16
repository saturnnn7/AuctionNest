import { Component, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { merge, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';

import { AuctionDto, AuctionFilterParams, AuctionStatus, PagedResponse } from '../../../core/models/auction.model';
import { CategoryDto } from '../../../core/models/category.model';
import { AuctionService } from '../../../core/services/auction.service';
import { CategoryService } from '../../../core/services/category.service';
import { AuctionCardComponent } from '../auction-card/auction-card.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

interface SortOption { value: string; sortBy: 'endsAt' | 'price'; sortDescending: boolean; }

const SORT_OPTIONS: SortOption[] = [
  { value: 'endsAt_asc',  sortBy: 'endsAt', sortDescending: false },
  { value: 'endsAt_desc', sortBy: 'endsAt', sortDescending: true  },
  { value: 'price_asc',   sortBy: 'price',  sortDescending: false },
  { value: 'price_desc',  sortBy: 'price',  sortDescending: true  },
];

const PAGE_SIZE = 12;

@Component({
  selector: 'app-auction-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatPaginatorModule,
    MatIconModule,
    TranslatePipe,
    AuctionCardComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
  ],
  templateUrl: './auction-list.component.html',
  styleUrl: './auction-list.component.scss',
})
export class AuctionListComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auctionService = inject(AuctionService);
  private categoryService = inject(CategoryService);
  private destroyRef = inject(DestroyRef);

  // Page state
  auctions = signal<AuctionDto[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  categories = signal<CategoryDto[]>([]);
  activeCount = signal(0);
  currentPage = signal(1);
  readonly pageSize = PAGE_SIZE;
  readonly pageSizeOptions = [12, 24, 48];
  readonly sortOptions = SORT_OPTIONS;

  // Filter form
  filterForm = new FormGroup({
    search:     new FormControl('', { nonNullable: true }),
    categoryId: new FormControl('', { nonNullable: true }),
    status:     new FormControl('', { nonNullable: true }),
    sortBy:     new FormControl('endsAt_asc', { nonNullable: true }),
  });

  // Subject to trigger API loads, enabling switchMap cancellation
  private loadTrigger$ = new Subject<Params>();

  constructor() {
    // Load categories once
    this.categoryService.getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(c => this.categories.set(c));

    // Load active count for hero
    this.auctionService.getAuctions({ status: 'Active', page: 1, pageSize: 1 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(r => this.activeCount.set(r.totalCount));

    // API call stream (switchMap cancels in-flight requests on new trigger)
    this.loadTrigger$.pipe(
      tap(() => this.loading.set(true)),
      switchMap(params => this.auctionService.getAuctions(this.buildFilters(params))),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (res: PagedResponse<AuctionDto>) => {
        this.auctions.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });

    // URL query params → sync form + trigger load
    const queryParams$ = this.route.queryParams;
    queryParams$.pipe(
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(params => {
      this.syncFormFromParams(params);
      this.currentPage.set(parseInt(params['page'] || '1', 10));
      this.loadTrigger$.next(params);
    });

    // Search → debounce 400ms → update URL (resets to page 1)
    this.filterForm.controls.search.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(v => this.updateUrl({ search: v || null, page: null }));

    // Other filters → immediate URL update (resets to page 1)
    merge(
      this.filterForm.controls.categoryId.valueChanges,
      this.filterForm.controls.status.valueChanges,
      this.filterForm.controls.sortBy.valueChanges,
    ).pipe(takeUntilDestroyed(this.destroyRef))
     .subscribe(() => this.onNonSearchFilterChange());
  }

  onPageChange(event: PageEvent): void {
    this.updateUrl({ page: event.pageIndex + 1, pageSize: event.pageSize !== PAGE_SIZE ? event.pageSize : null });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  private onNonSearchFilterChange(): void {
    const v = this.filterForm.getRawValue();
    const sort = SORT_OPTIONS.find(o => o.value === v.sortBy) ?? SORT_OPTIONS[0];
    this.updateUrl({
      categoryId:    v.categoryId  || null,
      status:        v.status      || null,
      sortBy:        sort.sortBy,
      sortDescending: sort.sortDescending || null,
      page:          null,
    });
  }

  private syncFormFromParams(params: Params): void {
    const sortCombo = this.buildSortCombo(params['sortBy'], params['sortDescending']);
    this.filterForm.patchValue({
      search:     params['search']     || '',
      categoryId: params['categoryId'] || '',
      status:     params['status']     || '',
      sortBy:     sortCombo,
    }, { emitEvent: false });
  }

  private buildSortCombo(sortBy: string | undefined, sortDescending: string | undefined): string {
    if (!sortBy) return 'endsAt_asc';
    const desc = sortDescending === 'true';
    return `${sortBy}_${desc ? 'desc' : 'asc'}`;
  }

  private buildFilters(params: Params): AuctionFilterParams {
    const page = parseInt(params['page'] || '1', 10);
    const pageSize = parseInt(params['pageSize'] || String(PAGE_SIZE), 10);
    return {
      page,
      pageSize,
      search:        params['search']        || undefined,
      categoryId:    params['categoryId']    || undefined,
      status:        (params['status'] as AuctionStatus) || undefined,
      sortBy:        (params['sortBy'] as 'endsAt' | 'price') || undefined,
      sortDescending: params['sortDescending'] === 'true' || undefined,
    };
  }

  private updateUrl(params: Record<string, unknown>): void {
    this.router.navigate([], {
      queryParams: params,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
