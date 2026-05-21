import { Component, HostListener, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { filter } from 'rxjs/operators';
import { AuthService } from '../core/auth/auth.service';
import { ThemeService } from '../core/theme/theme.service';
import { RemovalHistoryService } from '../core/removal/removal-history.service';
import { UserAvatarComponent } from './user-avatar.component';

@Component({
  selector: 'app-nav',
  imports: [RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatIconModule, MatTooltipModule, UserAvatarComponent],
  template: `
    <mat-toolbar color="primary" class="topbar">
      <button mat-icon-button class="hamburger" (click)="toggleDrawer()" aria-label="Abrir menu">
        <mat-icon>menu</mat-icon>
      </button>

      <mat-icon class="brand-icon">library_music</mat-icon>
      <span class="brand">SpotifyDel</span>

      <nav class="nav-inline">
        <a mat-button routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
          <mat-icon>dashboard</mat-icon>
          Dashboard
        </a>
        <a mat-button routerLink="/liked" routerLinkActive="active">
          <mat-icon>favorite</mat-icon>
          Curtidas
        </a>
        <a mat-button routerLink="/playlists" routerLinkActive="active">
          <mat-icon>queue_music</mat-icon>
          Playlists
        </a>
        <a mat-button routerLink="/triage" routerLinkActive="active">
          <mat-icon>cleaning_services</mat-icon>
          Triagem
        </a>
      </nav>

      <span class="spacer"></span>

      <button mat-icon-button (click)="exportRemoved()"
              [disabled]="removal.entries().length === 0"
              [matTooltip]="removal.entries().length === 0 ? 'Sem remoções pra exportar' : 'Exportar ' + removal.entries().length + ' música(s) removida(s) em JSON'">
        <mat-icon>download</mat-icon>
      </button>

      <button mat-icon-button (click)="theme.toggle()"
              [matTooltip]="theme.isDark() ? 'Modo claro' : 'Modo escuro'">
        <mat-icon>{{ theme.isDark() ? 'light_mode' : 'dark_mode' }}</mat-icon>
      </button>

      @if (user()) {
        <span class="user-name">{{ user()!.displayName }}</span>
        <app-user-avatar [url]="user()!.avatarUrl" [name]="user()!.displayName" [size]="32" />
      }
      <button mat-icon-button (click)="logout()" matTooltip="Sair">
        <mat-icon>logout</mat-icon>
      </button>
    </mat-toolbar>

    @if (drawerOpen()) {
      <div class="drawer-backdrop" (click)="closeDrawer()" aria-hidden="true"></div>
    }
    <aside class="drawer" [class.open]="drawerOpen()" role="navigation" aria-label="Menu de navegação">
      <div class="drawer-header">
        @if (user()) {
          <app-user-avatar [url]="user()!.avatarUrl" [name]="user()!.displayName" [size]="48" />
          <div class="drawer-user">
            <div class="drawer-user-name">{{ user()!.displayName }}</div>
            <div class="drawer-user-hint">Conectado</div>
          </div>
        }
      </div>

      <nav class="drawer-nav">
        <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
          <mat-icon>dashboard</mat-icon>
          <span>Dashboard</span>
        </a>
        <a routerLink="/liked" routerLinkActive="active">
          <mat-icon>favorite</mat-icon>
          <span>Curtidas</span>
        </a>
        <a routerLink="/playlists" routerLinkActive="active">
          <mat-icon>queue_music</mat-icon>
          <span>Playlists</span>
        </a>
        <a routerLink="/triage" routerLinkActive="active">
          <mat-icon>cleaning_services</mat-icon>
          <span>Triagem</span>
        </a>
      </nav>

      <div class="drawer-footer">
        <button mat-stroked-button (click)="exportRemoved()" [disabled]="removal.entries().length === 0">
          <mat-icon>download</mat-icon>
          Exportar removidas
        </button>
        <button mat-stroked-button (click)="theme.toggle()">
          <mat-icon>{{ theme.isDark() ? 'light_mode' : 'dark_mode' }}</mat-icon>
          {{ theme.isDark() ? 'Modo claro' : 'Modo escuro' }}
        </button>
        <button mat-stroked-button color="warn" (click)="logout()">
          <mat-icon>logout</mat-icon>
          Sair
        </button>
      </div>
    </aside>
  `,
  styles: [`
    .topbar {
      position: sticky;
      top: 0;
      z-index: 10;
      gap: 0.5rem;
    }
    .hamburger { display: none; }
    .brand-icon { margin-right: 0.25rem; }
    .brand     { font-weight: 600; margin-right: 1rem; }
    .nav-inline { display: flex; gap: 0.25rem; }
    .nav-inline a.active { background: rgba(255, 255, 255, 0.15); }
    .spacer    { flex: 1 1 auto; }
    .user-name { font-size: 0.9rem; margin: 0 0.5rem; }

    .drawer-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.5);
      z-index: 20;
      animation: fade-in 0.2s ease-out;
    }
    @keyframes fade-in {
      from { opacity: 0; }
      to   { opacity: 1; }
    }

    .drawer {
      position: fixed;
      top: 0;
      left: 0;
      bottom: 0;
      width: min(80vw, 320px);
      background: var(--app-surface, #1e1e1e);
      color: var(--app-text, #fff);
      border-right: 1px solid var(--app-border, rgba(255,255,255,0.08));
      z-index: 21;
      transform: translateX(-100%);
      transition: transform 0.25s ease-out;
      display: flex;
      flex-direction: column;
      box-shadow: 2px 0 24px rgba(0, 0, 0, 0.35);
    }
    .drawer.open { transform: translateX(0); }

    .drawer-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 1.25rem 1rem 1rem;
      border-bottom: 1px solid var(--app-border, rgba(255,255,255,0.08));
    }
    .drawer-user-name { font-weight: 600; font-size: 0.95rem; }
    .drawer-user-hint { font-size: 0.75rem; opacity: 0.6; }

    .drawer-nav {
      display: flex;
      flex-direction: column;
      padding: 0.5rem 0;
      flex: 1 1 auto;

      a {
        display: flex;
        align-items: center;
        gap: 0.85rem;
        padding: 0.85rem 1.25rem;
        color: inherit;
        text-decoration: none;
        font-weight: 500;
        font-size: 0.95rem;
        border-left: 3px solid transparent;
        transition: background 0.15s, border-color 0.15s;

        &:hover { background: var(--app-hover, rgba(255,255,255,0.05)); }
        &.active {
          background: var(--app-hover, rgba(29,185,84,0.10));
          border-left-color: var(--app-accent, #1db954);
          color: var(--app-accent, #1db954);
        }
        mat-icon { font-size: 22px; width: 22px; height: 22px; }
      }
    }

    .drawer-footer {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      padding: 1rem;
      border-top: 1px solid var(--app-border, rgba(255,255,255,0.08));

      button {
        justify-content: flex-start;
        ::ng-deep .mat-icon { margin-right: 0.5rem; }
      }
    }

    @media (max-width: 768px) {
      .hamburger { display: inline-flex; }
      .brand { margin-right: auto; font-size: 1.05rem; }
      .nav-inline { display: none; }
      .user-name { display: none; }

      .topbar { padding: 0 0.5rem; }
    }

    @media (min-width: 769px) {
      .drawer, .drawer-backdrop { display: none; }
    }
  `],
})
export class AppNavComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly theme = inject(ThemeService);
  readonly removal = inject(RemovalHistoryService);

  readonly user = this.auth.user;
  readonly drawerOpen = signal(false);

  constructor() {
    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.closeDrawer());
  }

  toggleDrawer() { this.drawerOpen.update(v => !v); }
  closeDrawer()  { this.drawerOpen.set(false); }

  @HostListener('document:keydown.escape')
  onEscape() { this.closeDrawer(); }

  exportRemoved() {
    this.removal.exportJson();
    this.closeDrawer();
  }

  logout() {
    this.closeDrawer();
    this.auth.logout().subscribe({
      next: () => this.router.navigateByUrl('/login'),
      error: () => this.router.navigateByUrl('/login'),
    });
  }
}
