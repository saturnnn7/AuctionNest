import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'auction_theme';

  isDark = signal<boolean>(this.loadPreference());

  constructor() {
    // Apply saved/OS preference immediately on service creation
    this.applyTheme();
  }

  toggle(): void {
    this.isDark.update(v => !v);
    this.applyTheme();
    localStorage.setItem(this.STORAGE_KEY, this.isDark() ? 'dark' : 'light');
  }

  private applyTheme(): void {
    const body = document.body;
    if (this.isDark()) {
      body.classList.remove('light-theme');
      body.classList.add('dark-theme', 'dark'); // 'dark' activates Tailwind dark: utilities
    } else {
      body.classList.remove('dark-theme', 'dark');
      body.classList.add('light-theme');
    }
  }

  private loadPreference(): boolean {
    const stored = localStorage.getItem(this.STORAGE_KEY);
    if (stored) return stored === 'dark';
    // Fall back to OS preference
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }
}
