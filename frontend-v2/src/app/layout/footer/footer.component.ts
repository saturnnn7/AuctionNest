import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink],
  // CCD: Minimal footer — no distractions from the auction experience
  template: `
    <footer style="border-top: 1px solid var(--color-border); margin-top: auto;">
      <div style="max-width: 1280px; margin: 0 auto; padding: 20px 1rem;
                  display: flex; align-items: center; justify-content: space-between;
                  font-size: 13px; color: var(--color-muted);">
        <span>© {{ year }} AuctionNest</span>
        <div style="display: flex; gap: 16px;">
          <a routerLink="/" style="color: var(--color-muted); text-decoration: none;">Terms</a>
          <a routerLink="/" style="color: var(--color-muted); text-decoration: none;">Privacy</a>
        </div>
      </div>
    </footer>
  `,
})
export class FooterComponent {
  readonly year = new Date().getFullYear();
}
