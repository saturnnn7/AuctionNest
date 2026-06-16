export type AuctionStatus = 'Draft' | 'Active' | 'Extending' | 'Ended' | 'Cancelled';

export interface AuctionDto {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  sellerId: string;
  sellerDisplayName: string;
  categoryId: string;
  categoryName: string;
  startPrice: number;
  currentPrice: number;
  reservePrice: number | null;
  buyItNowPrice: number | null;
  minBidIncrement: number;
  status: AuctionStatus;
  startsAt: string;
  endsAt: string;
  extensionCount: number;
  bidCount: number;
  isReserveMet: boolean;
  isBuyItNowAvailable: boolean;
}

export interface AuctionDetailDto extends AuctionDto {
  recentBids: ApiBidDto[];
  totalBids: number;
}

export interface ApiBidDto {
  id: string;
  auctionId: string;
  bidderId: string;
  amount: number;
  isWinning: boolean;
  newCurrentPrice: number;
  placedAt: string;
}

export interface BidDto {
  id: string;
  auctionId: string;
  bidderId: string;
  bidderDisplayName: string;
  amount: number;
  isWinning: boolean;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface AuctionFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  status?: AuctionStatus;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: 'endsAt' | 'price';
  sortDescending?: boolean;
}

export interface CreateAuctionRequest {
  categoryId: string;
  title: string;
  description: string;
  startPrice: number;
  minBidIncrement: number;
  startsAt: string;
  endsAt: string;
  reservePrice?: number;
  buyItNowPrice?: number;
  imageUrl?: string;
}
