import { Component } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  template: `
    <div class="flex items-center justify-center p-8">
      <mat-spinner diameter="48"></mat-spinner>
    </div>
  `,
})
export class LoadingSpinnerComponent {}
