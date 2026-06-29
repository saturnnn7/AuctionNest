export type NotificationType = 'outbid' | 'won' | 'ending_soon' | 'auction_ended';

export interface AppNotification {
  id: string;
  type: NotificationType;
  message: string;
  auctionId: string;
  createdAt: string;
  isRead: boolean;
}
