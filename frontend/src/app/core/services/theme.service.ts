import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private _isDark = signal(this.getInitialTheme());
  readonly isDark = this._isDark.asReadonly();

  constructor() {
    this.applyTheme(this._isDark());
  }

  toggle(): void {
    this._isDark.update(v => !v);
    this.applyTheme(this._isDark());
    localStorage.setItem('theme', this._isDark() ? 'dark' : 'light');
  }

  private getInitialTheme(): boolean {
    const saved = localStorage.getItem('theme');
    if (saved) return saved === 'dark';
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }

  private applyTheme(isDark: boolean): void {
    document.body.classList.toggle('dark-theme', isDark);
    document.body.classList.toggle('light-theme', !isDark);
  }
}
