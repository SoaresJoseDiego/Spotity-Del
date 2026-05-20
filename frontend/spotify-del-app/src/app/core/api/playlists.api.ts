import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LikedTrack, Page, Playlist } from '../models/track.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PlaylistsApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBase;

  list(offset: number, limit: number): Observable<Page<Playlist>> {
    const params = new HttpParams().set('offset', offset).set('limit', limit);
    return this.http.get<Page<Playlist>>(`${this.base}/api/playlists`, { params });
  }

  tracks(playlistId: string, offset: number, limit: number): Observable<Page<LikedTrack>> {
    const params = new HttpParams().set('offset', offset).set('limit', limit);
    return this.http.get<Page<LikedTrack>>(
      `${this.base}/api/playlists/${encodeURIComponent(playlistId)}/tracks`,
      { params }
    );
  }

  removeTracks(playlistId: string, ids: string[]): Observable<void> {
    return this.http.delete<void>(
      `${this.base}/api/playlists/${encodeURIComponent(playlistId)}/tracks`,
      { body: { ids } }
    );
  }
}
