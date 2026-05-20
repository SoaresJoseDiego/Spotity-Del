import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';

import { TriageApi } from '../../core/api/triage.api';
import { LibrarySnapshot, RemovalItem, TriageTrack } from '../../core/models/triage.model';
import { AppNavComponent } from '../../shared/app-nav.component';
import { RemovalHistoryService } from '../../core/removal/removal-history.service';

@Component({
  selector: 'app-triage-page',
  imports: [
    DatePipe, FormsModule, AppNavComponent,
    MatButtonModule, MatIconModule, MatCheckboxModule, MatProgressBarModule,
    MatTooltipModule, MatSnackBarModule, MatFormFieldModule, MatInputModule, MatChipsModule,
  ],
  templateUrl: './triage-page.component.html',
  styleUrl: './triage-page.component.scss',
})
export class TriagePageComponent {
  private readonly api = inject(TriageApi);
  private readonly snack = inject(MatSnackBar);
  private readonly history = inject(RemovalHistoryService);

  readonly scanning = signal(false);
  readonly removing = signal(false);
  readonly snapshot = signal<LibrarySnapshot | null>(null);

  readonly onlyDuplicates = signal(false);
  readonly minArtistOccurrences = signal<number | null>(null);
  readonly search = signal('');

  readonly selected = signal<Set<string>>(new Set());

  readonly filtered = computed<TriageTrack[]>(() => {
    const snap = this.snapshot();
    if (!snap) return [];

    const onlyDup = this.onlyDuplicates();
    const minArtist = this.minArtistOccurrences() ?? 0;
    const term = this.search().trim().toLowerCase();

    const artistCount = new Map<string, number>();
    for (const t of snap.tracks)
      for (const a of t.artists)
        artistCount.set(a.id, (artistCount.get(a.id) ?? 0) + 1);

    return snap.tracks.filter(t => {
      if (onlyDup && t.originsCount < 2) return false;
      if (minArtist > 1) {
        const maxOcc = Math.max(0, ...t.artists.map(a => artistCount.get(a.id) ?? 0));
        if (maxOcc < minArtist) return false;
      }
      if (term) {
        const hay = (t.name + ' ' + t.artists.map(a => a.name).join(' ')).toLowerCase();
        if (!hay.includes(term)) return false;
      }
      return true;
    });
  });

  readonly selectedCount = computed(() => this.selected().size);
  readonly originsToRemove = computed(() => {
    const sel = this.selected();
    const snap = this.snapshot();
    if (!snap) return 0;
    let n = 0;
    for (const t of snap.tracks) {
      if (!sel.has(t.id)) continue;
      n += t.originsCount;
    }
    return n;
  });
  readonly allFilteredSelected = computed(() => {
    const sel = this.selected();
    const f = this.filtered();
    return f.length > 0 && f.every(t => sel.has(t.id));
  });

  scan() {
    this.scanning.set(true);
    this.api.scan().subscribe({
      next: snap => {
        this.snapshot.set(snap);
        this.scanning.set(false);
        this.snack.open(
          `Scan completo: ${snap.tracks.length} faixas únicas em ${snap.likedCount} curtidas + ${snap.playlistCount} playlists.`,
          'OK', { duration: 5000 });
      },
      error: (err) => {
        this.scanning.set(false);
        const msg = err?.error?.message ? `Erro ${err.error.status}: ${err.error.message}` : 'Erro ao escanear.';
        this.snack.open(msg, 'OK', { duration: 8000 });
      },
    });
  }

  toggle(t: TriageTrack, checked: boolean) {
    this.selected.update(set => {
      const next = new Set(set);
      if (checked) next.add(t.id); else next.delete(t.id);
      return next;
    });
  }
  isSelected(id: string) { return this.selected().has(id); }
  clearSelection() { this.selected.set(new Set()); }

  toggleAllFiltered(checked: boolean) {
    this.selected.update(set => {
      const next = new Set(set);
      for (const t of this.filtered()) {
        if (checked) next.add(t.id); else next.delete(t.id);
      }
      return next;
    });
  }

  removeSelected() {
    const sel = this.selected();
    const snap = this.snapshot();
    if (!snap || sel.size === 0) return;

    const items: RemovalItem[] = snap.tracks
      .filter(t => sel.has(t.id))
      .map(t => ({
        trackId: t.id,
        removeFromLiked: t.inLiked,
        removeFromPlaylistIds: t.inPlaylists.filter(p => p.canEdit).map(p => p.id),
      }));

    const origins = items.reduce((sum, i) => sum + (i.removeFromLiked ? 1 : 0) + i.removeFromPlaylistIds.length, 0);
    const ok = window.confirm(
      `Remover ${items.length} faixa(s) de ${origins} origens (curtidas + playlists)? Isso não pode ser desfeito automaticamente.`
    );
    if (!ok) return;

    this.removing.set(true);
    const snapshotTracks = snap.tracks.filter(t => sel.has(t.id));
    const removedAt = new Date().toISOString();
    this.api.remove(items).subscribe({
      next: result => {
        this.removing.set(false);
        const removedIds = new Set(items.map(i => i.trackId));
        this.snapshot.update(s => s ? {
          ...s,
          tracks: s.tracks.filter(t => !removedIds.has(t.id)),
        } : s);
        this.selected.set(new Set());
        this.history.record(snapshotTracks.map(t => ({
          trackId: t.id,
          trackName: t.name,
          artists: t.artists.map(a => a.name).join(', '),
          removedAt,
          source: 'triage' as const,
          sourceLabel: t.inLiked
            ? `liked + ${t.inPlaylists.length} playlist(s)`
            : `${t.inPlaylists.length} playlist(s)`,
        })));

        let msg = `Removidas: ${result.likedRemoved} das curtidas + ${result.playlistTracksRemoved} de playlists.`;
        if (result.failures.length > 0) msg += ` ${result.failures.length} falha(s).`;
        this.snack.open(msg, 'OK', { duration: 6000 });
      },
      error: err => {
        this.removing.set(false);
        const msg = err?.error?.message ? `Erro ${err.error.status}: ${err.error.message}` : 'Erro ao remover.';
        this.snack.open(msg, 'OK', { duration: 8000 });
      },
    });
  }

  formatDuration(ms: number): string {
    const total = Math.floor(ms / 1000);
    const m = Math.floor(total / 60);
    const s = total % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }
}
