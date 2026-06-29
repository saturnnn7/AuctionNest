import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  LoginRequest, RegisterRequest,
  AuthResponse, AuthUser,
  REFRESH_TOKEN_KEY,
} from '@core/models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private http = inject(HttpClient);

    // Access token lives in memory. Lost on F5, restored by APP_INITIALIZER
    private _accessToken    = signal<string | null>(null);
    private _user           = signal<AuthUser | null>(null);

    // Public read-only access
    readonly accessToken        = this._accessToken.asReadonly();
    readonly user               = this._user.asReadonly();
    readonly isAuthenticated    = computed(() => this._accessToken() !== null);

    login(request: LoginRequest): Observable<AuthResponse> {
        return this.http
            .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, request)
            .pipe(tap(r => this.setSession(r)))
    }

    register(request: RegisterRequest): Observable<AuthResponse> {
        return this.http
            .post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, request)
            .pipe(tap(r => this.setSession(r)))
    }

    refresh(): Observable<AuthResponse> {
        const rt = localStorage.getItem(REFRESH_TOKEN_KEY);
        if (!rt) return throwError(() => Error('No refresh token'));

        return this.http
            .post<AuthResponse>(`${environment.apiUrl}/api/auth/refresh`, { refreshToken: rt })
            .pipe(
                tap(r => this.setSession(r)),
            catchError(err => {
                this.clearSession();
                return throwError(() => err);
            })
            );
    }

    logout(): void {
        this.clearSession();
    }

    setSession(response: AuthResponse): void {
        this._accessToken.set(response.accessToken);
        this._user.set({
            userId:      response.userId,
            username:    response.username,
            displayName: response.displayName,
            role:        response.role,
        });
        localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    }

    clearSession(): void {
        this._accessToken.set(null);
        this._user.set(null);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
    }
}