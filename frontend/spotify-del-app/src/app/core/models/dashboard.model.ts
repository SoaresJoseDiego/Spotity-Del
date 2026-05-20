import { Artist } from './track.model';

export type TimeRange = 'short_term' | 'medium_term' | 'long_term';

export interface TopArtist {
  id: string;
  name: string;
  imageUrl: string | null;
  genres: string[];
  popularity: number;
  followers: number;
}

export interface TopTrack {
  id: string;
  name: string;
  durationMs: number;
  externalUrl: string;
  albumName: string;
  albumImageUrl: string | null;
  artists: Artist[];
}

export interface GenreSlice {
  genre: string;
  count: number;
}

export interface DashboardInsight {
  label: string;
  value: string;
  hint: string | null;
}

export interface RecentPlay {
  trackId: string;
  trackName: string;
  albumName: string;
  albumImageUrl: string | null;
  artists: Artist[];
  externalUrl: string;
  playedAt: string;
}

export interface DashboardOverview {
  likedTotal: number;
  playlistTotal: number;
  timeRange: TimeRange;
  topArtists: TopArtist[];
  topTracks: TopTrack[];
  genres: GenreSlice[];
  insights: DashboardInsight[];
  recentPlays: RecentPlay[];
}
