import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LibrarySnapshot, RemovalItem, RemovalResult } from '../models/triage.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TriageApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBase;

  scan(): Observable<LibrarySnapshot> {
    return this.http.post<LibrarySnapshot>(`${this.base}/api/triage/scan`, {});
  }

  remove(items: RemovalItem[]): Observable<RemovalResult> {
    return this.http.post<RemovalResult>(`${this.base}/api/triage/remove`, { items });
  }
}
