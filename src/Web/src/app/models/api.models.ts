// TypeScript mirrors of the backend DTOs. Single source of truth for the
// services and components. Property names match the API's camelCase JSON.

export enum SourceType {
  Rss = 0,
  Atom = 1,
  YouTube = 2,
  Reddit = 3,
}

/** Mirrors Models/TriageState.cs — values must stay aligned. */
export enum TriageState {
  New = 0,
  Kept = 1,
  Dismissed = 2,
}

export interface Source {
  id: number;
  feedUrl: string;
  title: string;
  siteUrl?: string | null;
  type: SourceType;
  category?: string | null;
  addedUtc: string;
  lastFetchedUtc?: string | null;
}

export interface Item {
  id: number;
  sourceId: number;
  guid?: string | null;
  url: string;
  title: string;
  author?: string | null;
  summary?: string | null;
  publishedUtc?: string | null;
  fetchedUtc: string;
  isRead: boolean;
  triageState: TriageState;
}

/** An item enriched with its source's title, for views that show the origin feed. */
export interface ItemWithSource extends Item {
  sourceTitle: string;
}

/** Paginated response from GET /api/items. */
export interface ItemsPage {
  items: Item[];
  hasMore: boolean;
  nextCursor: string | null;
}

/** Query params for GET /api/items. */
export interface ItemsPageParams {
  triage?: TriageState[];
  sourceId?: number;
  limit?: number;
  cursor?: string;
}

export interface AppSettingsResponse {
  lastOpenedUtc: string | null;
}

export interface CreateSourceRequest {
  feedUrl: string;
  title?: string;
  siteUrl?: string;
  type?: SourceType;
  category?: string;
}
