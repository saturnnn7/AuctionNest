import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';
import { REFRESH_TOKEN_KEY } from '@core/models/auth.model';

export const authInterceptorFn: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthService);

    // Attach Bearer token if available
    const token = auth.accessToken();
    const authReq = token 
        ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : req;

        return next(authReq).pipe(
            catchError((error: HttpErrorResponse) => {
                const canRetry = 
                    error.status === 401 &&
                    !req.url.includes('/api/auth/refresh') &&
                    !!localStorage.getItem(REFRESH_TOKEN_KEY);
                
                if (!canRetry) return throwError(() => error);
                
                return auth.refresh().pipe(
                    switchMap(() => {
                        const newToken = auth.accessToken();
                        return next(req.clone({
                            setHeaders: { Authorization: `Bearer ${newToken}` }
                        }));
                    }),
                    catchError(refreshErr => {
                        auth.logout();
                        return throwError(() => refreshErr);
                    })
                );
            })
        );
};