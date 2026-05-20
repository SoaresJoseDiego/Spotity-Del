import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../core/auth/auth.service';
import { ThemeService } from '../core/theme/theme.service';
import { RemovalHistoryService } from '../core/removal/removal-history.service';
import { UserAvatarComponent } from './user-avatar.component';

@Component({
  selector: 'app-nav',
  imports: [RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatIconModule, MatTooltipModule, UserAvatarComponent],
  template: `
    <mat-toolbar color="primary" class="topbar">
      <mat-icon class="brand-icon">library_music</mat-icon>
      <span class="brand">SpotifyDel</span>

      <nav class="nav">
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
  `,
  styles: [`
    .topbar {
      position: sticky;
      top: 0;
      z-index: 10;
      gap: 0.5rem;
    }
    .brand-icon { margin-right: 0.25rem; }
    .brand     { font-weight: 600; margin-right: 1rem; }
    .nav       { display: flex; gap: 0.25rem; }
    .nav a.active {
      background: rgba(255, 255, 255, 0.15);
    }
    .spacer    { flex: 1 1 auto; }
    .user-name { font-size: 0.9rem; margin: 0 0.5rem; }
    .avatar    { width: 32px; height: 32px; border-radius: 50%; object-fit: cover; }
  `],
})
export class AppNavComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly theme = inject(ThemeService);
  readonly removal = inject(RemovalHistoryService);

  readonly user = this.auth.user;

  exportRemoved() { this.removal.exportJson(); }

  logout() {
    this.auth.logout().subscribe({
      next: () => this.router.navigateByUrl('/login'),
      error: () => this.router.navigateByUrl('/login'),
    });
  }
}
