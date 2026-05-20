export interface Artist {
  id: string;
  name: string;
}

export interface Album {
  id: string;
  name: string;
  imageUrl: string | null;
}

export interface LikedTrack {
  id: string;
  name: string;
  durationMs: number;
  previewUrl: string | null;
  externalUrl: string;
  addedAt: string;
  album: Album;
  artists: Artist[];
}

export interface Page<T> {
  items: T[];
  offset: number;
  limit: number;
  total: number;
  hasMore: boolean;
}

export interface FilterMatch {
  track: LikedTrack;
  reasons: string[];
}

export interface FilterRequest {
  addedBefore?: string;
  minArtistOccurrences?: number;
  excludeGenres?: string[];
}

export interface UserProfile {
  id: string;
  displayName: string;
  avatarUrl: string | null;
}

export interface Playlist {
  id: string;
  name: string;
  description: string | null;
  imageUrl: string | null;
  ownerId: string;
  ownerName: string;
  trackCount: number;
  isCollaborative: boolean;
  isPublic: boolean;
  canEdit: boolean;
}
