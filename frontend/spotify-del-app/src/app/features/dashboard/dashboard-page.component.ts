import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { DashboardApi } from '../../core/api/dashboard.api';
import { DashboardOverview, TimeRange } from '../../core/models/dashboard.model';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/theme/theme.service';
import { AppNavComponent } from '../../shared/app-nav.component';
import { UserAvatarComponent } from '../../shared/user-avatar.component';
import { SkeletonComponent } from '../../shared/skeleton.component';
import { cascadeIn } from '../../shared/animations';
import { ShareDialogComponent } from './share-dialog.component';

@Component({
  selector: 'app-dashboard-page',
  imports: [
    DecimalPipe, AppNavComponent, UserAvatarComponent, SkeletonComponent,
    MatButtonModule, MatIconModule, MatButtonToggleModule, MatDialogModule,
    MatProgressBarModule, MatTooltipModule,
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  animations: [cascadeIn],
})
export class DashboardPageComponent implements OnInit {
  private readonly api = inject(DashboardApi);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  readonly theme = inject(ThemeService);

  readonly user = this.auth.user;
  readonly loading = signal(false);
  readonly data = signal<DashboardOverview | null>(null);
  readonly timeRange = signal<TimeRange>('medium_term');

  readonly topGenres = computed(() => {
    const d = this.data();
    return d ? d.genres.slice(0, 8) : [];
  });

  constructor() {
    effect(() => {
      const _ = this.timeRange();
      this.load();
    });
  }

  ngOnInit() { /* load triggered by effect on timeRange */ }

  load() {
    this.loading.set(true);
    this.api.overview(this.timeRange()).subscribe({
      next: d => { this.data.set(d); this.loading.set(false); },
      error: () => { this.loading.set(false); },
    });
  }

  setTimeRange(tr: TimeRange) { this.timeRange.set(tr); }

  openShare() {
    const overview = this.data();
    if (!overview) return;
    this.dialog.open(ShareDialogComponent, {
      data: { overview, userName: this.user()?.displayName ?? null },
      maxWidth: '95vw',
      width: '720px',
    });
  }

  timeRangeLabel(): string {
    switch (this.timeRange()) {
      case 'short_term':  return 'últimas 4 semanas';
      case 'medium_term': return 'últimos 6 meses';
      case 'long_term':   return 'desde sempre';
    }
  }

  formatDuration(ms: number): string {
    const total = Math.floor(ms / 1000);
    const m = Math.floor(total / 60);
    const s = total % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  capitalize(s: string): string {
    return s.length === 0 ? s : s[0].toUpperCase() + s.slice(1);
  }
}
