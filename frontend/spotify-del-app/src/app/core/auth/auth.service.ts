import { Injectable, computed, inject, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import { AuthApi } from '../api/auth.api';
import { UserProfile } from '../models/track.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApi);

  private readonly _user = signal<UserProfile | null>(null);
  private readonly _initialized = signal(false);

  readonly user = this._user.asReadonly();
  readonly initialized = this._initialized.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  initialize() {
    return this.api.me().pipe(
      tap(profile => this._user.set(profile)),
      catchError(() => {
        this._user.set(null);
        return of(null);
      }),
      tap(() => this._initialized.set(true))
    );
  }

  startLogin() {
    window.location.href = this.api.loginUrl();
  }

  logout() {
    return this.api.logout().pipe(tap(() => this._user.set(null)));
  }
}
