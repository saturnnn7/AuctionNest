export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
}

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  username: string;
  displayName: string;
  role: string;
}

export interface AuthUser {
  userId: string;
  username: string;
  displayName: string;
  role: string;
}

export const REFRESH_TOKEN_KEY = 'auction_rt';