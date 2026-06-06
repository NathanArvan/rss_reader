// TypeScript mirrors of the backend DTOs. Single source of truth for the
// services and components. Property names match the API's camelCase JSON.

export enum SourceType {
  Rss = 0,
  Atom = 1,
  YouTube = 2,
  Reddit = 3,
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
}

/** An item enriched with its source's title, for the combined "All items" feed. */
export interface ItemWithSource extends Item {
  sourceTitle: string;
}

export interface CreateSourceRequest {
  feedUrl: string;
  title?: string;
  siteUrl?: string;
  type?: SourceType;
  category?: string;
}

/** Options for listing a source's items (mirrors the API query params). */
export interface ListItemsOptions {
  unreadOnly?: boolean;
  limit?: number;
  before?: string;
}
