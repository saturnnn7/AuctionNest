import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, CurrentUser, LoginRequest, RegisterRequest } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private _accessToken = signal<string | null>(null);
  private _currentUser = signal<CurrentUser | null>(null);

  readonly accessToken = this._accessToken.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._accessToken() !== null);

  async login(request: LoginRequest): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, request)
    );
    this.storeAuth(res);
  }

  async register(request: RegisterRequest): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, request)
    );
    this.storeAuth(res);
  }

  async silentRefresh(refreshToken: string): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/refresh`, { refreshToken })
    );
    this.storeAuth(res);
  }

  logout(): void {
    this._accessToken.set(null);
    this._currentUser.set(null);
    localStorage.removeItem('refreshToken');
    this.router.navigate(['/']);
  }

  updateDisplayName(displayName: string): void {
    const current = this._currentUser();
    if (current) this._currentUser.set({ ...current, displayName });
  }

  private storeAuth(res: AuthResponse): void {
    this._accessToken.set(res.accessToken);
    this._currentUser.set({
      userId: res.userId,
      username: res.username,
      displayName: res.displayName,
      role: res.role,
    });
    localStorage.setItem('refreshToken', res.refreshToken);
  }
}
