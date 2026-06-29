import { ApplicationConfig, APP_INITIALIZER } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideTranslateService, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { authInterceptorFn } from '@core/interceptors/auth.interceptor';
import { appInitializerFactory } from '@core/initializers/app.initializer';
import { AuthService } from '@core/services/auth.service';

export function HttpLoaderFactory(http: HttpClient) {
    return new TranslateHttpLoader(http, '/assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(routes, withComponentInputBinding()), 
        provideHttpClient(
            withInterceptors([authInterceptorFn])
        ),

        provideAnimationsAsync(),

        provideTranslateService({
            loader: {
                provide: TranslateLoader,
                useFactory: HttpLoaderFactory,
                deps: [HttpClient],
            },
            defaultLanguage: 'en',
        }),

        {
            provide: APP_INITIALIZER,
            useFactory: appInitializerFactory,
            deps: [AuthService],
            multi: true,
        },
    ],
};