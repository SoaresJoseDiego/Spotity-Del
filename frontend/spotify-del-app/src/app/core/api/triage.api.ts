import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LibrarySnapshot, RemovalItem, RemovalResult } from '../models/triage.model';

@Injectable({ providedIn: 'root' })
export class TriageApi {
  private readonly http = inject(HttpClient);

  scan(): Observable<LibrarySnapshot> {
    return this.http.post<LibrarySnapshot>('/api/triage/scan', {});
  }

  remove(items: RemovalItem[]): Observable<RemovalResult> {
    return this.http.post<RemovalResult>('/api/triage/remove', { items });
  }
}
