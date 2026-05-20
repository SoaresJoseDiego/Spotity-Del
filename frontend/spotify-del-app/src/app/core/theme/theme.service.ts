import { Injectable, effect, signal } from '@angular/core';

const STORAGE_KEY = 'spotifydel.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _isDark = signal<boolean>(this.initial());
  readonly isDark = this._isDark.asReadonly();

  constructor() {
    effect(() => {
      const dark = this._isDark();
      document.documentElement.classList.toggle('dark', dark);
      try { localStorage.setItem(STORAGE_KEY, dark ? 'dark' : 'light'); } catch { /* private mode */ }
    });
  }

  toggle() { this._isDark.update(v => !v); }
  set(dark: boolean) { this._isDark.set(dark); }

  private initial(): boolean {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'dark') return true;
      if (stored === 'light') return false;
    } catch { /* private mode */ }
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }
}
