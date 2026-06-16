import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="flex flex-col items-center justify-center py-16 text-center">
      <mat-icon class="text-gray-400 dark:text-slate-500 mb-4" style="font-size:48px;width:48px;height:48px">
        {{ icon() }}
      </mat-icon>
      <p class="text-lg text-gray-500 dark:text-slate-400">{{ message() }}</p>
    </div>
  `,
})
export class EmptyStateComponent {
  message = input<string>('No results found.');
  icon = input<string>('inbox');
}
