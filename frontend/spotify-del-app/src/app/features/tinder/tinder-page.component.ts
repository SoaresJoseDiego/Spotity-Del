import { Component, ElementRef, OnDestroy, computed, inject, signal, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { TracksApi } from '../../core/api/tracks.api';
import { LikedTrack, Page } from '../../core/models/track.model';
import { AppNavComponent } from '../../shared/app-nav.component';
import { SkeletonComponent } from '../../shared/skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { RemovalHistoryService } from '../../core/removal/removal-history.service';

type Decision = 'remove' | 'keep';
interface Verdict { track: LikedTrack; decision: Decision }

const SWIPE_THRESHOLD = 110;

@Component({
  selector: 'app-tinder-page',
  imports: [
    AppNavComponent, SkeletonComponent, EmptyStateComponent,
    MatButtonModule, MatIconModule, MatProgressBarModule, MatTooltipModule, MatSnackBarModule,
  ],
  templateUrl: './tinder-page.component.html',
  styleUrl: './tinder-page.component.scss',
})
export class TinderPageComponent implements OnDestroy {
  private readonly api = inject(TracksApi);
  private readonly snack = inject(MatSnackBar);
  private readonly history = inject(RemovalHistoryService);

  readonly audioRef = viewChild<ElementRef<HTMLAudioElement>>('audio');

  readonly queue = signal<LikedTrack[]>([]);
  readonly verdicts = signal<Verdict[]>([]);
  readonly loading = signal(false);
  readonly removing = signal(false);
  readonly started = signal(false);

  readonly current = computed(() => this.queue()[0] ?? null);
  readonly upcoming = computed(() => this.queue().slice(1, 3));
  readonly toRemove = computed(() => this.verdicts().filter(v => v.decision === 'remove'));
  readonly done = computed(() => this.queue().length === 0 && this.verdicts().length > 0);

  readonly dragX = signal(0);
  readonly isDragging = signal(false);
  readonly isPlaying = signal(false);

  readonly hint = computed<'remove' | 'keep' | null>(() => {
    const dx = this.dragX();
    if (dx < -40) return 'remove';
    if (dx > 40) return 'keep';
    return null;
  });

  readonly transform = computed(() => {
    const dx = this.dragX();
    const rot = dx / 18;
    return `translateX(${dx}px) rotate(${rot}deg)`;
  });

  private dragStart: { x: number; y: number; pointerId: number } | null = null;

  start() {
    this.started.set(true);
    this.loading.set(true);
    this.queue.set([]);
    this.verdicts.set([]);
    this.loadPage(0);
  }

  backToIntro() {
    this.started.set(false);
    this.queue.set([]);
    this.verdicts.set([]);
  }

  private loadPage(offset: number) {
    this.api.liked(offset, 50).subscribe({
      next: (page: Page<LikedTrack>) => {
        this.queue.update(curr => [...curr, ...page.items]);
        if (page.hasMore && this.queue().length < 100) {
          this.loadPage(offset + page.items.length);
        } else {
          this.loading.set(false);
        }
      },
      error: () => {
        this.loading.set(false);
        this.snack.open('Erro ao carregar curtidas.', 'OK', { duration: 4000 });
      },
    });
  }

  swipeRemove() { this.decide('remove'); }
  swipeKeep()   { this.decide('keep'); }

  undo() {
    if (this.verdicts().length === 0) return;
    const last = this.verdicts().slice(-1)[0];
    this.verdicts.update(v => v.slice(0, -1));
    this.queue.update(q => [last.track, ...q]);
    this.pauseAudio();
  }

  private decide(decision: Decision) {
    const t = this.current();
    if (!t) return;
    this.pauseAudio();
    this.verdicts.update(v => [...v, { track: t, decision }]);
    this.queue.update(q => q.slice(1));
    this.dragX.set(0);
  }

  // ----- Pointer-drag swipe -----
  onPointerDown(e: PointerEvent) {
    if (!this.current()) return;
    this.dragStart = { x: e.clientX, y: e.clientY, pointerId: e.pointerId };
    this.isDragging.set(true);
    (e.target as HTMLElement).setPointerCapture(e.pointerId);
  }
  onPointerMove(e: PointerEvent) {
    if (!this.dragStart || this.dragStart.pointerId !== e.pointerId) return;
    this.dragX.set(e.clientX - this.dragStart.x);
  }
  onPointerUp(e: PointerEvent) {
    if (!this.dragStart || this.dragStart.pointerId !== e.pointerId) return;
    const dx = e.clientX - this.dragStart.x;
    this.dragStart = null;
    this.isDragging.set(false);
    if (dx < -SWIPE_THRESHOLD) this.swipeRemove();
    else if (dx > SWIPE_THRESHOLD) this.swipeKeep();
    else this.dragX.set(0);
  }

  // ----- Audio preview -----
  togglePlay() {
    const audio = this.audioRef()?.nativeElement;
    const track = this.current();
    if (!audio || !track?.previewUrl) return;

    if (this.isPlaying()) {
      audio.pause();
      this.isPlaying.set(false);
    } else {
      audio.play().then(() => this.isPlaying.set(true)).catch(() => this.isPlaying.set(false));
    }
  }
  pauseAudio() {
    const audio = this.audioRef()?.nativeElement;
    if (audio && !audio.paused) audio.pause();
    this.isPlaying.set(false);
  }

  // ----- Bulk remove at end -----
  commitRemovals() {
    const list = this.toRemove();
    if (list.length === 0) return;

    const ok = window.confirm(`Remover ${list.length} música(s) das suas Curtidas?`);
    if (!ok) return;

    this.removing.set(true);
    const ids = list.map(v => v.track.id);
    const removedAt = new Date().toISOString();
    this.api.remove(ids).subscribe({
      next: () => {
        this.history.record(list.map(v => ({
          trackId: v.track.id,
          trackName: v.track.name,
          artists: v.track.artists.map(a => a.name).join(', '),
          removedAt,
          source: 'liked' as const,
          sourceLabel: 'tinder',
        })));
        this.snack.open(`${list.length} música(s) removidas.`, 'OK', { duration: 5000 });
        this.removing.set(false);
        this.queue.set([]);
        this.verdicts.set([]);
      },
      error: err => {
        this.removing.set(false);
        const msg = err?.error?.message ? `Erro: ${err.error.message}` : 'Erro ao remover.';
        this.snack.open(msg, 'OK', { duration: 7000 });
      },
    });
  }

  reset() {
    this.queue.set([]);
    this.verdicts.set([]);
    this.started.set(false);
  }

  ngOnDestroy() { this.pauseAudio(); }

  backStackTransform(i: number): string {
    const scale = 0.92 - i * 0.04;
    const translate = 12 + i * 8;
    return `scale(${scale}) translateY(${translate}px)`;
  }

  formatDuration(ms: number): string {
    const total = Math.floor(ms / 1000);
    const m = Math.floor(total / 60);
    const s = total % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }
}
