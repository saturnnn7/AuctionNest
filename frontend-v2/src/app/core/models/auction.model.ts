export type AuctionStatus = 'Active' | 'Ended' | 'Cancelled' | 'Pending';

export interface AuctionSummaryDto {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  startingPrice: number;
  currentPrice: number;
  buyItNowPrice: number | null;
  status: AuctionStatus;
  categoryId: string;
  categoryName: string;
  sellerId: string;
  sellerDisplayName: string;
  startTime: string;
  endTime: string;
  totalBids: number;   // NOT bidCount - real API field name
}

export interface AuctionDetailDto extends AuctionSummaryDto {
  recentBids: ApiBidDto[];  // NOT "bids"
}

export interface ApiBidDto {
  id: string;
  auctionId: string;
  bidderId: string;          // no bidderDisplayName in response
  amount: number;
  isWinning: boolean;
  newCurrentPrice: number;
  placedAt: string;          // NOT createdAt
}

export interface BidPlacedEvent {
  auctionId: string;
  bidderId: string;
  amount: number;
  newCurrentPrice: number;
  occurredAt: string;        // no bidId, no bidderDisplayName
}

export interface AuctionEndedEvent {
  auctionId: string;
  winnerId: string | null;
  finalPrice: number;
  occurredAt: string;
}

export interface AuctionExtendedEvent {
  auctionId: string;
  newEndTime: string;
}

export interface AuctionCancelledEvent {
  auctionId: string;
  reason: string;
  occurredAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuctionFilters {
  page: number;
  pageSize: number;
  search: string;
  categoryId: string;
  status: string;
  minPrice: number | null;
  maxPrice: number | null;
  sortBy: string;
  sortDescending: boolean;
}

export const DEFAULT_FILTERS: AuctionFilters = {
  page: 1,
  pageSize: 12,
  search: '',
  categoryId: '',
  status: '',
  minPrice: null,
  maxPrice: null,
  sortBy: 'createdAt',
  sortDescending: true,
};

export interface CategoryDto {
  id: string;
  name: string;
}