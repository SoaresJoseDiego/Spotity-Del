import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserProfile } from '../models/track.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBase;

  me(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.base}/api/auth/me`);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.base}/api/auth/logout`, {});
  }

  loginUrl(): string {
    return `${this.base}/api/auth/login`;
  }
}
