import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <footer class="mt-auto py-6 border-t text-center text-sm text-gray-500">
      &copy; {{ year }} AuctionNest. All rights reserved.
    </footer>
  `,
})
export class FooterComponent {
  readonly year = new Date().getFullYear();
}
