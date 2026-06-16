export interface BidPlacedEvent {
  bidId?: string;              // not in current backend event; guard with ??
  auctionId: string;
  bidderId: string;
  bidderDisplayName?: string;  // not in current backend event; guard with ??
  amount: number;
  newCurrentPrice: number;
  occurredAt: string;
}

export interface AuctionExtendedEvent {
  auctionId: string;
  newEndsAt: string;
  extensionCount: number;
}

export interface AuctionEndedEvent {
  auctionId: string;
  winnerId: string | null;
  winningAmount: number | null;
  isReserveMet: boolean;
}

export interface AuctionCancelledEvent {
  auctionId: string;
}
