import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ThemeService } from '@core/services/theme.service';
import { HeaderComponent } from './layout/header/header.component';
import { FooterComponent } from './layout/footer/footer.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent],
  // CCD: Full-height layout with sticky header keeps nav always accessible
  template: `
    <div style="min-height: 100vh; display: flex; flex-direction: column;">
      <app-header />
      <main style="flex: 1; background: var(--color-bg);">
        <router-outlet />
      </main>
      <app-footer />
    </div>
  `,
})
export class AppComponent implements OnInit {
  private translate    = inject(TranslateService);
  // Inject ThemeService to apply saved theme on first render
  protected themeService = inject(ThemeService);

  ngOnInit(): void {
    this.translate.setDefaultLang('en');
    this.translate.use('en');
  }
}
