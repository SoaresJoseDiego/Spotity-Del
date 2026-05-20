import { Component, Input, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { PlaylistsApi } from '../../core/api/playlists.api';
import { LikedTrack, Page } from '../../core/models/track.model';
import { AppNavComponent } from '../../shared/app-nav.component';
import { RemovalHistoryService } from '../../core/removal/removal-history.service';

@Component({
  selector: 'app-playlist-detail',
  imports: [
    DatePipe, AppNavComponent,
    MatButtonModule, MatIconModule, MatCheckboxModule,
    MatProgressBarModule, MatTooltipModule, MatSnackBarModule,
  ],
  template: `
    <app-nav />
    @if (loading() || removing()) { <mat-progress-bar mode="indeterminate" /> }

    <div class="header">
      <button mat-icon-button (click)="back()" matTooltip="Voltar">
        <mat-icon>arrow_back</mat-icon>
      </button>
      <h1>{{ total() }} faixa(s) nesta playlist</h1>
    </div>

    @if (selectedCount() > 0) {
      <div class="selection-bar">
        <span>{{ selectedCount() }} selecionada(s)</span>
        <button mat-button (click)="clearSelection()">Limpar</button>
        <button mat-flat-button color="warn" [disabled]="removing()" (click)="removeSelected()">
          <mat-icon>delete</mat-icon>
          Remover desta playlist
        </button>
      </div>
    }

    <div class="track-list">
      @for (t of tracks(); track t.id) {
        <div class="track" [class.selected]="isSelected(t.id)">
          <mat-checkbox [checked]="isSelected(t.id)" (change)="toggle(t, $event.checked)" />
          @if (t.album.imageUrl) {
            <img [src]="t.album.imageUrl" [alt]="t.album.name" class="cover" />
          } @else {
            <div class="cover cover-placeholder"><mat-icon>music_note</mat-icon></div>
          }
          <div class="meta">
            <a [href]="t.externalUrl" target="_blank" rel="noopener" class="title">{{ t.name }}</a>
            <div class="artists">
              @for (a of t.artists; track a.id) {
                <span>{{ a.name }}</span>@if (!$last) { <span>, </span> }
              }
              <span class="album"> · {{ t.album.name }}</span>
            </div>
          </div>
          <div class="extra">
            <span>{{ formatDuration(t.durationMs) }}</span>
            <span class="added">{{ t.addedAt | date:'dd MMM yyyy' }}</span>
          </div>
        </div>
      } @empty {
        @if (!loading()) { <div class="empty">Playlist sem faixas.</div> }
      }
    </div>

    @if (hasMore() && tracks().length > 0) {
      <div class="load-more">
        <button mat-stroked-button (click)="loadMore()" [disabled]="loading()">
          Carregar mais ({{ total() - tracks().length }} restantes)
        </button>
      </div>
    }
  `,
  styleUrl: '../tracks/tracks-page.component.scss',
})
export class PlaylistDetailComponent implements OnInit {
  @Input() id!: string;

  private readonly api = inject(PlaylistsApi);
  private readonly snack = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly history = inject(RemovalHistoryService);
  private readonly pageSize = 100;

  readonly tracks = signal<LikedTrack[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly removing = signal(false);
  readonly hasMore = signal(true);
  readonly selected = signal<Set<string>>(new Set());

  readonly selectedCount = computed(() => this.selected().size);

  ngOnInit() { this.loadMore(); }

  loadMore() {
    if (this.loading() || !this.hasMore()) return;
    this.loading.set(true);
    this.api.tracks(this.id, this.tracks().length, this.pageSize).subscribe({
      next: (page: Page<LikedTrack>) => {
        this.tracks.update(curr => [...curr, ...page.items]);
        this.total.set(page.total);
        this.hasMore.set(page.hasMore);
        this.loading.set(false);
      },
      error: (err) => {
        const msg = err?.error?.message
          ? `Erro ${err.error.status}: ${err.error.message}`
          : 'Erro ao carregar faixas.';
        this.snack.open(msg, 'OK', { duration: 8000 });
        this.loading.set(false);
      },
    });
  }

  toggle(t: LikedTrack, checked: boolean) {
    this.selected.update(set => {
      const next = new Set(set);
      if (checked) next.add(t.id); else next.delete(t.id);
      return next;
    });
  }
  isSelected(id: string) { return this.selected().has(id); }
  clearSelection() { this.selected.set(new Set()); }

  removeSelected() {
    const ids = Array.from(this.selected());
    if (ids.length === 0) return;
    if (!confirm(`Remover ${ids.length} faixa(s) desta playlist?`)) return;

    this.removing.set(true);
    const snapshot = this.tracks().filter(t => this.selected().has(t.id));
    const removedAt = new Date().toISOString();
    this.api.removeTracks(this.id, ids).subscribe({
      next: () => {
        const removedSet = new Set(ids);
        this.tracks.update(list => list.filter(t => !removedSet.has(t.id)));
        this.total.update(n => Math.max(0, n - ids.length));
        this.selected.set(new Set());
        this.removing.set(false);
        this.history.record(snapshot.map(t => ({
          trackId: t.id,
          trackName: t.name,
          artists: t.artists.map(a => a.name).join(', '),
          removedAt,
          source: 'playlist' as const,
          sourceLabel: `playlist ${this.id}`,
        })));
        this.snack.open(`${ids.length} faixa(s) removida(s) da playlist.`, 'OK', { duration: 4000 });
      },
      error: (err) => {
        this.removing.set(false);
        const msg = err?.error?.message
          ? `Erro ${err.error.status}: ${err.error.message}`
          : err?.status === 403
            ? 'Você não pode editar esta playlist (não é o dono).'
            : 'Erro ao remover.';
        this.snack.open(msg, 'OK', { duration: 8000 });
      },
    });
  }

  back() { this.router.navigateByUrl('/playlists'); }

  formatDuration(ms: number): string {
    const total = Math.floor(ms / 1000);
    const m = Math.floor(total / 60);
    const s = total % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }
}
