import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './layout/header/header.component';
import { FooterComponent } from './layout/footer/footer.component';
import { ThemeService } from './core/services/theme.service';
import { WatchlistService } from './core/services/watchlist.service';
import { SignalrService } from './core/services/signalr.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  // Eagerly inject root services so their constructor effects fire on app startup
  private theme = inject(ThemeService);
  private watchlist = inject(WatchlistService);
  private signalr = inject(SignalrService);
}
