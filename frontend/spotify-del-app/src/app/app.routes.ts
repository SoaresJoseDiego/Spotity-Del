import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard-page.component').then(m => m.DashboardPageComponent),
  },
  {
    path: 'liked',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tracks/tracks-page.component').then(m => m.TracksPageComponent),
  },
  {
    path: 'playlists',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/playlists/playlists-list.component').then(m => m.PlaylistsListComponent),
  },
  {
    path: 'playlists/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/playlists/playlist-detail.component').then(m => m.PlaylistDetailComponent),
  },
  {
    path: 'triage',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/triage/triage-page.component').then(m => m.TriagePageComponent),
  },
  { path: '**', redirectTo: '' },
];
