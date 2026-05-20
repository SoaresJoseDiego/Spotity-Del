import { Component, computed, effect, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="login-page">
      <div class="ambient"></div>

      <div class="brand-block">
        <div class="logo">
          <mat-icon>library_music</mat-icon>
        </div>
        <div class="equalizer" aria-hidden="true">
          @for (b of bars; track b) {
            <span class="bar" [style.animation-delay.ms]="b"></span>
          }
        </div>
      </div>

      <h1>SpotifyDel</h1>
      <p class="tagline">Faxina inteligente das suas <strong>curtidas</strong> e <strong>playlists</strong>.</p>

      <ul class="features">
        <li><mat-icon>cleaning_services</mat-icon><span>Triagem cruzando curtidas + playlists</span></li>
        <li><mat-icon>insights</mat-icon><span>Dashboard com seus tops e gêneros</span></li>
        <li><mat-icon>delete_sweep</mat-icon><span>Remover em lote, com filtros inteligentes</span></li>
      </ul>

      <button mat-flat-button color="primary" class="cta" (click)="login()">
        <mat-icon>login</mat-icon>
        Entrar com Spotify
      </button>

      @if (errorMessage()) { <p class="error">{{ errorMessage() }}</p> }

      <p class="footer-note">
        Conexão direta via OAuth do Spotify. Seu token fica criptografado e nunca sai do servidor.
      </p>
    </div>
  `,
  styles: [`
    :host { display: block; }

    .login-page {
      position: relative;
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 2rem 1.5rem;
      color: #fff;
      background: #0b0d0f;
      overflow: hidden;
      text-align: center;
    }
    .ambient {
      position: absolute;
      inset: -10%;
      background:
        radial-gradient(40% 30% at 20% 30%, rgba(29,185,84,0.45),  transparent 60%),
        radial-gradient(35% 25% at 80% 20%, rgba(124,92,255,0.40), transparent 60%),
        radial-gradient(40% 30% at 60% 90%, rgba(33,150,243,0.35), transparent 60%);
      filter: blur(28px);
      animation: drift 18s ease-in-out infinite alternate;
      z-index: 0;
    }
    @keyframes drift {
      0%   { transform: translate(0, 0)        scale(1); }
      50%  { transform: translate(-2%, 3%)     scale(1.06); }
      100% { transform: translate(3%, -2%)     scale(1.02); }
    }

    .brand-block, h1, .tagline, .features, .cta, .error, .footer-note {
      position: relative;
      z-index: 1;
    }

    .brand-block {
      display: flex;
      align-items: end;
      gap: 0.75rem;
      margin-bottom: 1rem;
    }
    .logo {
      width: 72px; height: 72px;
      border-radius: 22px;
      display: grid; place-items: center;
      background: linear-gradient(135deg, #1db954, #1ed760);
      box-shadow: 0 12px 32px rgba(29,185,84,0.45);
      mat-icon { font-size: 38px; width: 38px; height: 38px; color: #0a0a0a; }
    }

    .equalizer {
      display: flex;
      align-items: end;
      gap: 4px;
      height: 36px;
    }
    .equalizer .bar {
      width: 4px;
      background: #fff;
      border-radius: 2px;
      height: 30%;
      animation: bounce 1.2s ease-in-out infinite;
    }
    @keyframes bounce {
      0%, 100% { height: 20%; opacity: 0.6; }
      50%      { height: 100%; opacity: 1; }
    }

    h1 {
      font-size: 2.5rem;
      margin: 0 0 0.5rem;
      letter-spacing: -0.02em;
      font-weight: 600;
    }
    .tagline {
      margin: 0 0 1.75rem;
      font-size: 1.05rem;
      opacity: 0.85;
      max-width: 460px;
      strong { color: #1db954; }
    }

    .features {
      list-style: none;
      padding: 0;
      margin: 0 0 2rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      text-align: left;
      max-width: 360px;

      li {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        background: rgba(255,255,255,0.06);
        border: 1px solid rgba(255,255,255,0.08);
        backdrop-filter: blur(4px);
        padding: 0.6rem 0.85rem;
        border-radius: 10px;
        font-size: 0.9rem;

        mat-icon { color: #1db954; }
      }
    }

    .cta {
      padding: 0 1.5rem;
      height: 48px;
      font-size: 1rem;
      letter-spacing: 0.02em;
      box-shadow: 0 8px 24px rgba(29,185,84,0.35);
      ::ng-deep .mat-icon { margin-right: 0.5rem; }
    }

    .error {
      margin-top: 1rem;
      color: #ff8a80;
    }

    .footer-note {
      position: absolute;
      bottom: 1rem;
      font-size: 0.75rem;
      opacity: 0.55;
      max-width: 460px;
      margin: 0 auto;
    }

    @media (max-width: 540px) {
      h1 { font-size: 2rem; }
      .footer-note { position: static; margin-top: 2rem; }
    }
  `],
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly bars = [0, 120, 60, 200, 90, 160, 30];

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
