export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  username: string;
  displayName: string;
  role: string;
}

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
}

export interface CurrentUser {
  userId: string;
  username: string;
  displayName: string;
  role: string;
}

export interface UserProfileDto {
  userId: string;
  username: string;
  email: string;
  displayName: string;
  role: string;
}
