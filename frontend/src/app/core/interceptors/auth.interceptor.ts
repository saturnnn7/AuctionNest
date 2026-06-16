import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token) {
        const refreshToken = localStorage.getItem('refreshToken');
        if (refreshToken) {
          return from(auth.silentRefresh(refreshToken)).pipe(
            switchMap(() => {
              const newToken = auth.accessToken();
              const retryReq = newToken
                ? req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } })
                : req;
              return next(retryReq);
            }),
            catchError(refreshError => {
              auth.logout();
              return throwError(() => refreshError);
            })
          );
        }
        auth.logout();
      }
      return throwError(() => error);
    })
  );
};
