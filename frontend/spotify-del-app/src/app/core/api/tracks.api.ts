import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FilterMatch, FilterRequest, LikedTrack, Page } from '../models/track.model';

@Injectable({ providedIn: 'root' })
export class TracksApi {
  private readonly http = inject(HttpClient);

  liked(offset: number, limit: number): Observable<Page<LikedTrack>> {
    const params = new HttpParams().set('offset', offset).set('limit', limit);
    return this.http.get<Page<LikedTrack>>('/api/tracks/liked', { params });
  }

  remove(ids: string[]): Observable<void> {
    return this.http.delete<void>('/api/tracks/liked', { body: { ids } });
  }

  filter(request: FilterRequest): Observable<FilterMatch[]> {
    return this.http.post<FilterMatch[]>('/api/tracks/filter', request);
  }
}
