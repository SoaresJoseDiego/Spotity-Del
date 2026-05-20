import { Component, OnInit, ViewChildren, QueryList, computed, effect, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';

import { DashboardApi } from '../../core/api/dashboard.api';
import { DashboardOverview, TimeRange } from '../../core/models/dashboard.model';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/theme/theme.service';
import { AppNavComponent } from '../../shared/app-nav.component';
import { UserAvatarComponent } from '../../shared/user-avatar.component';

@Component({
  selector: 'app-dashboard-page',
  imports: [
    DecimalPipe, AppNavComponent, UserAvatarComponent, BaseChartDirective,
    MatButtonModule, MatIconModule, MatButtonToggleModule,
    MatProgressBarModule, MatTooltipModule,
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
})
export class DashboardPageComponent implements OnInit {
  private readonly api = inject(DashboardApi);
  private readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);

  @ViewChildren(BaseChartDirective) private readonly charts!: QueryList<BaseChartDirective>;

  readonly user = this.auth.user;
  readonly loading = signal(false);
  readonly data = signal<DashboardOverview | null>(null);
  readonly timeRange = signal<TimeRange>('medium_term');

  readonly genreChartData = computed<ChartData<'doughnut'>>(() => {
    const d = this.data();
    if (!d || d.genres.length === 0) return { labels: [], datasets: [{ data: [] }] };
    return {
      labels: d.genres.map(g => this.capitalize(g.genre)),
      datasets: [{
        data: d.genres.map(g => g.count),
        backgroundColor: [
          '#1db954', '#7c5cff', '#ff6f61', '#ffb74d', '#4fc3f7',
          '#ba68c8', '#aed581', '#f06292', '#4dd0e1', '#ffd54f',
        ],
        borderColor: 'transparent',
        hoverOffset: 8,
      }],
    };
  });

  readonly genreChartOptions = computed<ChartConfiguration<'doughnut'>['options']>(() => {
    const isDark = this.theme.isDark();
    const text = isDark ? 'rgba(255,255,255,0.85)' : 'rgba(0,0,0,0.85)';
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: 'right',
          labels: { color: text, boxWidth: 12, padding: 12 },
        },
        tooltip: {
          callbacks: {
            label: (ctx) => ` ${ctx.label}: ${ctx.parsed} artistas no top`,
          },
        },
      },
      cutout: '60%',
    };
  });

  readonly popularityChartData = computed<ChartData<'bar'>>(() => {
    const d = this.data();
    if (!d) return { labels: [], datasets: [{ data: [] }] };
    const top = d.topArtists.slice(0, 10);
    return {
      labels: top.map(a => a.name),
      datasets: [{
        data: top.map(a => a.popularity),
        label: 'Popularidade (0-100)',
        backgroundColor: '#1db954',
        borderRadius: 4,
      }],
    };
  });

  readonly popularityChartOptions = computed<ChartConfiguration<'bar'>['options']>(() => {
    const isDark = this.theme.isDark();
    const text = isDark ? 'rgba(255,255,255,0.85)' : 'rgba(0,0,0,0.85)';
    const grid = isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)';
    return {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: (ctx) => ` Popularidade: ${ctx.parsed.x}` } },
      },
      scales: {
        x: { beginAtZero: true, max: 100, ticks: { color: text }, grid: { color: grid } },
        y: { ticks: { color: text }, grid: { color: 'transparent' } },
      },
    };
  });

  constructor() {
    effect(() => {
      const _ = this.timeRange();
      this.load();
    });

    // Force-update the charts whenever data or theme changes; ng2-charts + Angular
    // signals don't always detect input changes on its own in v6.
    effect(() => {
      this.data();
      this.theme.isDark();
      queueMicrotask(() => this.charts?.forEach(c => c.update()));
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

  private capitalize(s: string): string {
    return s.length === 0 ? s : s[0].toUpperCase() + s.slice(1);
  }
}
