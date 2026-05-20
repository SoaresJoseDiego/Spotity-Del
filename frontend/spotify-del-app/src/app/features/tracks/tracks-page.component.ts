import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';

import { TracksApi } from '../../core/api/tracks.api';
import { FilterMatch, LikedTrack, Page } from '../../core/models/track.model';
import { FiltersDialogComponent } from './filters-dialog.component';
import { AppNavComponent } from '../../shared/app-nav.component';

@Component({
  selector: 'app-tracks-page',
  imports: [
    DatePipe, AppNavComponent,
    MatButtonModule, MatIconModule, MatCheckboxModule,
    MatProgressBarModule, MatSnackBarModule, MatDialogModule, MatTooltipModule,
  ],
  templateUrl: './tracks-page.component.html',
  styleUrl: './tracks-page.component.scss',
})
export class TracksPageComponent implements OnInit {
  private readonly api = inject(TracksApi);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  private readonly pageSize = 50;

  readonly tracks = signal<LikedTrack[]>([]);
  readonly selected = signal<Set<string>>(new Set());
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly removing = signal(false);
  readonly hasMore = signal(true);

  readonly selectedCount = computed(() => this.selected().size);
  readonly allOnPageSelected = computed(() => {
    const sel = this.selected();
    const list = this.tracks();
    return list.length > 0 && list.every(t => sel.has(t.id));
  });

  ngOnInit() { this.loadMore(); }

  loadMore() {
    if (this.loading() || !this.hasMore()) return;
    this.loading.set(true);
    this.api.liked(this.tracks().length, this.pageSize).subscribe({
      next: (page: Page<LikedTrack>) => {
        this.tracks.update(curr => [...curr, ...page.items]);
        this.total.set(page.total);
        this.hasMore.set(page.hasMore);
        this.loading.set(false);
      },
      error: () => {
        this.snack.open('Erro ao carregar curtidas.', 'OK', { duration: 4000 });
        this.loading.set(false);
      },
    });
  }

  toggle(track: LikedTrack, checked: boolean) {
    this.selected.update(set => {
      const next = new Set(set);
      if (checked) next.add(track.id); else next.delete(track.id);
      return next;
    });
  }

  isSelected(id: string): boolean { return this.selected().has(id); }

  toggleAllOnPage(checked: boolean) {
    this.selected.update(set => {
      const next = new Set(set);
      for (const t of this.tracks()) {
        if (checked) next.add(t.id); else next.delete(t.id);
      }
      return next;
    });
  }

  clearSelection() { this.selected.set(new Set()); }

  openFilters() {
    const ref = this.dialog.open(FiltersDialogComponent, { width: '480px' });
    ref.afterClosed().subscribe(request => {
      if (!request) return;
      this.loading.set(true);
      this.api.filter(request).subscribe({
        next: (matches: FilterMatch[]) => {
          this.loading.set(false);
          if (matches.length === 0) {
            this.snack.open('Nenhuma música bateu com os filtros.', 'OK', { duration: 4000 });
            return;
          }
          this.selected.update(set => {
            const next = new Set(set);
            for (const m of matches) next.add(m.track.id);
            return next;
          });
          this.snack.open(`${matches.length} música(s) marcadas pelos filtros.`, 'OK', { duration: 5000 });
        },
        error: () => {
          this.loading.set(false);
          this.snack.open('Erro ao aplicar filtros.', 'OK', { duration: 4000 });
        },
      });
    });
  }

  removeSelected() {
    const ids = Array.from(this.selected());
    if (ids.length === 0) return;

    const confirmed = window.confirm(
      `Remover ${ids.length} música(s) das suas Curtidas? Isso não pode ser desfeito automaticamente.`
    );
    if (!confirmed) return;

    this.removing.set(true);
    this.api.remove(ids).subscribe({
      next: () => {
        const removedSet = new Set(ids);
        this.tracks.update(list => list.filter(t => !removedSet.has(t.id)));
        this.total.update(n => Math.max(0, n - ids.length));
        this.selected.set(new Set());
        this.removing.set(false);
        this.snack.open(`${ids.length} música(s) removida(s).`, 'OK', { duration: 4000 });
      },
      error: (err) => {
        this.removing.set(false);
        const msg = err?.error?.message
          ? `Erro ${err.error.status}: ${err.error.message}`
          : 'Erro ao remover.';
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
