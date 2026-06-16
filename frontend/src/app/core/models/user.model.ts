export interface UserProfileDto {
  userId: string;
  username: string;
  email: string;
  displayName: string;
  role: string;
}

export interface WatchListDto {
  auctionId: string;
  addedAt: string;
}
