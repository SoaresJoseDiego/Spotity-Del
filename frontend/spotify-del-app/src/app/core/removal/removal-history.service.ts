import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'spotifydel.removal-history';
const MAX_ENTRIES = 5000;

export type RemovalSource = 'liked' | 'playlist' | 'triage';

export interface RemovalRecord {
  trackId: string;
  trackName: string;
  artists: string;
  removedAt: string;
  source: RemovalSource;
  sourceLabel?: string;
}

@Injectable({ providedIn: 'root' })
export class RemovalHistoryService {
  private readonly _entries = signal<RemovalRecord[]>(this.load());
  readonly entries = this._entries.asReadonly();

  record(items: RemovalRecord[]) {
    if (items.length === 0) return;
    this._entries.update(curr => {
      const next = [...items, ...curr];
      if (next.length > MAX_ENTRIES) next.length = MAX_ENTRIES;
      try { localStorage.setItem(STORAGE_KEY, JSON.stringify(next)); } catch {}
      return next;
    });
  }

  clear() {
    this._entries.set([]);
    try { localStorage.removeItem(STORAGE_KEY); } catch {}
  }

  exportJson(): void {
    const data = this._entries();
    const payload = {
      exportedAt: new Date().toISOString(),
      app: 'SpotifyDel',
      count: data.length,
      entries: data,
    };
    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `spotifydel-removed-${this.todayTag()}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  private load(): RemovalRecord[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed as RemovalRecord[] : [];
    } catch {
      return [];
    }
  }

  private todayTag(): string {
    const d = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }
}
