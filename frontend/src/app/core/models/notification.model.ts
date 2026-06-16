export type NotificationType =
  | 'BidOutbid'
  | 'AuctionWon'
  | 'AuctionEndedSeller'
  | 'ReserveMet'
  | 'WatchedAuctionEnding'
  | 'BuyItNowPurchased';

export interface NotificationDto {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  payload: string | null;
  isRead: boolean;
  createdAt: string;
}
