import { Component, computed, effect, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [MatButtonModule, MatCardModule, MatIconModule],
  template: `
    <div class="login-page">
      <mat-card class="login-card">
        <mat-card-header>
          <mat-card-title>SpotifyDel</mat-card-title>
          <mat-card-subtitle>Faxina inteligente das suas curtidas no Spotify</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>Conecte sua conta para listar curtidas, aplicar filtros e remover em lote.</p>
          @if (errorMessage()) {
            <p class="error">{{ errorMessage() }}</p>
          }
        </mat-card-content>
        <mat-card-actions>
          <button mat-flat-button color="primary" (click)="login()">
            <mat-icon>login</mat-icon>
            Entrar com Spotify
          </button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-page {
      display: grid;
      place-items: center;
      min-height: 100vh;
      padding: 1rem;
      background:
        radial-gradient(1200px 600px at 10% 0%, rgba(29,185,84,0.18), transparent 60%),
        radial-gradient(900px 500px at 100% 100%, rgba(120,80,200,0.15), transparent 55%),
        var(--app-bg);
    }
    .login-card {
      max-width: 420px;
      width: 100%;
      padding: 1rem;
    }
    .error {
      color: #ef5350;
      margin-top: 0.5rem;
    }
    button mat-icon { margin-right: 0.5rem; }
  `],
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly params = toSignal(this.route.queryParamMap);
  readonly errorMessage = computed(() => {
    const err = this.params()?.get('error');
    return err ? `Erro do Spotify: ${err}` : null;
  });

  constructor() {
    effect(() => {
      if (this.auth.initialized() && this.auth.isAuthenticated()) {
        this.router.navigateByUrl('/');
      }
    });
  }

  login() { this.auth.startLogin(); }
}
