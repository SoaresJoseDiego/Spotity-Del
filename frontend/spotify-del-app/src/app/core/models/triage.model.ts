import { Artist } from './track.model';

export interface TriagePlaylistRef {
  id: string;
  name: string;
  canEdit: boolean;
}

export interface TriageTrack {
  id: string;
  name: string;
  imageUrl: string | null;
  durationMs: number;
  externalUrl: string;
  artists: Artist[];
  likedAt: string | null;
  inLiked: boolean;
  inPlaylists: TriagePlaylistRef[];
  originsCount: number;
}

export interface LibrarySnapshot {
  likedCount: number;
  playlistCount: number;
  tracks: TriageTrack[];
}

export interface RemovalItem {
  trackId: string;
  removeFromLiked: boolean;
  removeFromPlaylistIds: string[];
}

export interface RemovalResult {
  likedRemoved: number;
  playlistTracksRemoved: number;
  failures: string[];
}
