import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { PlaylistsApi } from '../../core/api/playlists.api';
import { Page, Playlist } from '../../core/models/track.model';
import { AppNavComponent } from '../../shared/app-nav.component';

@Component({
  selector: 'app-playlists-list',
  imports: [
    RouterLink, AppNavComponent, MatButtonModule, MatIconModule,
    MatProgressBarModule, MatTooltipModule, MatSnackBarModule,
  ],
  template: `
    <app-nav />
    @if (loading()) { <mat-progress-bar mode="indeterminate" /> }

    <div class="page">
      <h1>Suas playlists ({{ total() }})</h1>

      <div class="grid">
        @for (p of playlists(); track p.id) {
          <a [routerLink]="['/playlists', p.id]" class="card" [class.readonly]="!p.canEdit">
            @if (p.imageUrl) {
              <img [src]="p.imageUrl" [alt]="p.name" class="cover" />
            } @else {
              <div class="cover cover-placeholder">
                <mat-icon>queue_music</mat-icon>
              </div>
            }
            <div class="meta">
              <div class="name">{{ p.name }}</div>
              <div class="sub">
                {{ p.trackCount }} faixa(s) · {{ p.ownerName }}
                @if (p.isCollaborative) { <span class="tag">colab</span> }
                @if (!p.canEdit) {
                  <span class="tag readonly-tag" matTooltip="Você não é dono, só dá pra ver">
                    <mat-icon inline>lock</mat-icon> read-only
                  </span>
                }
              </div>
            </div>
          </a>
        } @empty {
          @if (!loading()) {
            <div class="empty">Nenhuma playlist encontrada.</div>
          }
        }
      </div>

      @if (hasMore()) {
        <div class="load-more">
          <button mat-stroked-button (click)="loadMore()" [disabled]="loading()">
            Carregar mais
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; background: var(--app-bg); min-height: 100vh; color: var(--app-text); }
    .page { max-width: 1100px; margin: 1rem auto; padding: 0 1rem; }
    h1   { margin: 0.5rem 0 1rem; font-weight: 500; }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 1rem;
    }
    .card {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      background: var(--app-surface);
      border-radius: 8px;
      padding: 0.75rem;
      text-decoration: none;
      color: inherit;
      border: 1px solid var(--app-border);
      transition: transform .15s, box-shadow .15s;

      &:hover {
        transform: translateY(-2px);
        box-shadow: var(--app-shadow);
      }
      &.readonly { opacity: 0.7; }
    }
    .cover {
      width: 100%;
      aspect-ratio: 1 / 1;
      object-fit: cover;
      border-radius: 4px;
    }
    .cover-placeholder {
      display: grid; place-items: center;
      background: var(--app-hover); color: var(--app-text-muted);
    }
    .name {
      font-weight: 500;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .sub {
      font-size: 0.8rem;
      color: var(--app-text-muted);
      display: flex;
      align-items: center;
      gap: 0.25rem;
      flex-wrap: wrap;
    }
    .tag {
      background: var(--app-hover);
      border-radius: 999px;
      padding: 0 0.5rem;
      font-size: 0.7rem;
      display: inline-flex;
      align-items: center;
      gap: 0.15rem;
    }
    .readonly-tag mat-icon {
      font-size: 0.9rem; width: 0.9rem; height: 0.9rem;
    }
    .empty { padding: 3rem; text-align: center; color: var(--app-text-muted); grid-column: 1 / -1; }
    .load-more { text-align: center; padding: 1.5rem; }
  `],
})
export class PlaylistsListComponent implements OnInit {
  private readonly api = inject(PlaylistsApi);
  private readonly snack = inject(MatSnackBar);
  private readonly pageSize = 50;

  readonly playlists = signal<Playlist[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly hasMore = signal(true);

  ngOnInit() { this.loadMore(); }

  loadMore() {
    if (this.loading() || !this.hasMore()) return;
    this.loading.set(true);
    this.api.list(this.playlists().length, this.pageSize).subscribe({
      next: (page: Page<Playlist>) => {
        this.playlists.update(curr => [...curr, ...page.items]);
        this.total.set(page.total);
        this.hasMore.set(page.hasMore);
        this.loading.set(false);
      },
      error: () => {
        this.snack.open('Erro ao carregar playlists.', 'OK', { duration: 4000 });
        this.loading.set(false);
      },
    });
  }
}
