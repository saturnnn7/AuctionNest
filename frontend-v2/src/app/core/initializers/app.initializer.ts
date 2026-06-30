import { AuthService } from '@core/services/auth.service';
import { REFRESH_TOKEN_KEY } from '@core/models/auth.model';
import { firstValueFrom, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export function appInitializerFactory(auth: AuthService): () => Promise<void> {
    return async () => {
        if (!localStorage.getItem(REFRESH_TOKEN_KEY)) return;

        await firstValueFrom(
            auth.refresh().pipe(catchError(() => of(null)))
        );
    };
}