import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FilterMatch, FilterRequest, LikedTrack, Page } from '../models/track.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TracksApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBase;

  liked(offset: number, limit: number): Observable<Page<LikedTrack>> {
    const params = new HttpParams().set('offset', offset).set('limit', limit);
    return this.http.get<Page<LikedTrack>>(`${this.base}/api/tracks/liked`, { params });
  }

  remove(ids: string[]): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/tracks/liked`, { body: { ids } });
  }

  filter(request: FilterRequest): Observable<FilterMatch[]> {
    return this.http.post<FilterMatch[]>(`${this.base}/api/tracks/filter`, request);
  }
}
