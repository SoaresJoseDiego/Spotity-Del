import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LikedTrack, Page, Playlist } from '../models/track.model';

@Injectable({ providedIn: 'root' })
export class PlaylistsApi {
  private readonly http = inject(HttpClient);

  list(offset: number, limit: number): Observable<Page<Playlist>> {
    const params = new HttpParams().set('offset', offset).set('limit', limit);
    return this.http.get<Page<Playlist>>('/api/playlists', { params });
  }

  tracks(playlistId: string, offset: number, limit: number): Observable<Page<LikedTrack>> {
    const params = new HttpParams().set('offset', offset).set('limit', limit);
    return this.http.get<Page<LikedTrack>>(
      `/api/playlists/${encodeURIComponent(playlistId)}/tracks`,
      { params }
    );
  }

  removeTracks(playlistId: string, ids: string[]): Observable<void> {
    return this.http.delete<void>(
      `/api/playlists/${encodeURIComponent(playlistId)}/tracks`,
      { body: { ids } }
    );
  }
}
