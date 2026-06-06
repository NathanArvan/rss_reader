# Personal RSS Reader — Technical Challenges and Implementation Roadmap

## Stack

- **Frontend:** Angular PWA
- **Backend:** ASP.NET Core Web API
- **Database:** SQLite (probably) or PostgreSQL if sync becomes a goal
- **Target device:** Android tablet, primarily Chrome
- **Hosting:** Self-hosted (VPS, home server, or similar)

This is heavier than a minimum-viable RSS reader needs, but it's the stack you know well, which is the right tradeoff for a personal tool you actually want to finish.

---

## Technical Challenges

### 1. Cross-origin feed fetching

RSS feeds generally don't send CORS headers, so the Angular PWA cannot fetch them directly from the browser. The .NET backend must act as the feed fetcher, polling sources on a schedule and exposing a clean API to the frontend. This is actually a good thing — it gives you a place to do feed parsing, deduplication, full-text extraction, and YouTube metadata handling on the server where it belongs.

### 2. Feed polling and scheduling

The backend needs a background service that polls feeds on a schedule. ASP.NET Core's `IHostedService` or `BackgroundService` is the standard approach. Considerations:

- Per-feed polling intervals (a news site warrants more frequent polling than a low-volume blog)
- Respecting HTTP cache headers (`ETag`, `Last-Modified`) to avoid hammering servers
- Handling feeds that go away, change URLs, or start returning errors
- Backoff on failure so a broken feed doesn't get retried every minute forever

### 3. Feed parsing edge cases

RSS and Atom are both standards, both have variants, and real-world feeds violate both in creative ways. Don't write your own parser. For .NET, `System.ServiceModel.Syndication` handles the common cases; `CodeHollow.FeedReader` is more forgiving with malformed feeds. Expect to encounter:

- Feeds that claim to be RSS but are actually Atom (and vice versa)
- Invalid date formats
- HTML in fields that should be plain text
- Missing or duplicated GUIDs (the canonical item identifier)
- Encoding issues (especially in older feeds)

The deduplication strategy needs to handle missing GUIDs by falling back to URL or content hash.

### 4. YouTube channel feeds

YouTube channels have RSS feeds at `youtube.com/feeds/videos.xml?channel_id=...`. The challenges:

- Finding the channel ID from a channel URL — not always straightforward. Channel URLs come in several formats (`/channel/UC...`, `/c/CustomName`, `/@handle`, `/user/OldUsername`), and only the first directly contains the channel ID. The others require resolving via the YouTube page or API.
- The feed only returns the most recent 15 videos. For a personal reader this is fine; you're not building a YouTube archive.
- Video metadata in the feed is limited. Title, description, publication date, thumbnail URL, and video URL — no duration, view counts, or anything else. Duration requires the YouTube Data API, which has quota limits and requires an API key.

For your goals, the limited metadata is actually fine. You don't want view counts. You may or may not want duration.

### 5. Reddit RSS access

Reddit's RSS endpoints (`reddit.com/r/subname/.rss`) technically work but Reddit has been increasingly hostile to programmatic access. Rate limits are aggressive, user-agent checks are common, and the surface could break or require authentication at any time. Since the plan is to take a break from Reddit initially, this can be deferred entirely and revisited later.

### 6. Service worker and offline support

The PWA needs a service worker for installability and offline reading. Angular has built-in PWA support via `@angular/pwa`, which configures a service worker with reasonable defaults. The challenges:

- Caching strategy. Feed list and recent items should be cached for offline reading. Full article content (if you do extraction) should be cached opportunistically.
- Cache invalidation. When new content arrives, the service worker needs to update without forcing a full reload.
- Storage management. IndexedDB is the right place for feed/item data on the client. Don't try to fit it in localStorage.

### 7. Authentication

Even for a personal tool, the backend API needs authentication — otherwise anyone who finds the URL can see your reading habits. Simple options:

- Single-user with a long-lived bearer token stored in the PWA. Simplest approach for a one-user tool.
- ASP.NET Core Identity if you want a more standard setup, but it's overkill for one user.
- A reverse proxy (Caddy, nginx) handling authentication via basic auth or an SSO layer like Authelia. Moves the auth concern out of the application entirely.

For a personal tool, the reverse proxy approach is probably the cleanest.

### 8. Full-text extraction

RSS feeds often include only excerpts, not full article content. If you want to read full articles in the reader (rather than clicking through to the source), the backend needs to fetch the article URL and extract the main content. Mercury Parser is deprecated; Readability.NET is the .NET port of Mozilla's Readability algorithm and works reasonably well. This is a "nice to have" for v1 and a clear "v2 feature" if it gets complicated.

### 9. YouTube playback control

This is the messiest technical area for your stated goals. Options in order of control vs. complexity:

- **Iframe embed with API parameters.** The YouTube iframe player accepts URL parameters like `rel=0` (limit related videos) and `autoplay=0`. Some control, but not full — YouTube has been deprecating `rel=0` behavior over time, and end-screen suggestions still appear.
- **Iframe embed plus the IFrame Player API.** Gives you programmatic control over playback events. You can detect when a video ends and immediately navigate the user back to the reader rather than letting YouTube show suggestions. This is probably the right approach.
- **Hand off to Firefox with uBlock Origin and custom filter lists.** External to your app but gives you the most aggressive recommendation-hiding. Worth considering as a complement, not a replacement.
- **Hand off to the YouTube app.** Easiest, least control. Don't do this if avoiding YouTube's UI is a goal.

The iframe embed plus IFrame Player API is probably the right primary approach.

### 10. Session and limit tracking

Time limits and session tracking need to work even when the user navigates away and comes back. State needs to be persisted (probably server-side, so it can't be cleared by clearing browser data). The challenges:

- What counts as "active" time? Time the app is in the foreground? Time the user is scrolling? This affects how you instrument it.
- How are limits enforced? Soft (a warning that can be dismissed) or hard (content hidden until the next period)? The design doc suggests both, with the hard limit being changeable but with a 24-hour delay.
- What about content opened outside the reader (YouTube app, browser)? The reader can only track what flows through it. External enforcement (network-level blocking) is the answer for that, and it's out of scope for the reader itself.

### 11. Deployment

Self-hosted .NET deployment options:

- Docker container on a VPS. Most flexible, slight overhead.
- Direct `dotnet publish` to a VPS with systemd. Simpler, less isolation.
- Home server (Synology, Unraid, Raspberry Pi). No hosting cost but requires the server to be reliable and reachable.

If the tablet is going to access this from outside your home network (during travel), you need either a public-facing deployment or a VPN solution like Tailscale. Tailscale is probably the cleanest answer for a personal tool — your tablet and home server are on the same private network regardless of where you are, and you don't have to expose anything to the public internet.

---

## Suggested Implementation Order

The order is chosen to get to a working tool you actually use as quickly as possible, then iterate. Don't build everything in the design doc before you start using it — use it as soon as the core loop works, and let real usage drive what to build next.

### Phase 1: Minimum viable reader (target: usable in 2–3 weekends)

Goal: you can add a feed URL, the backend polls it, you can see new items in the PWA, you can mark them as read. That's it.

1. **Set up the project skeleton.** Angular app with `@angular/pwa` configured, ASP.NET Core Web API, SQLite for storage, basic auth (or no auth on localhost). Get a "hello world" PWA installing on your tablet.
2. **Feed storage and basic API.** Schema for sources and items. Endpoints to list sources, list items per source, mark items as read, add/remove a source.
3. **Feed fetching.** `BackgroundService` that polls feeds on a fixed interval (start with hourly for everything; refine later). Use `CodeHollow.FeedReader` or similar. Deduplicate by GUID, fall back to URL.
4. **Basic PWA UI.** List of sources, list of items per source, item detail view that links out to the source. No categories, no fancy session management, just the loop.
5. **Deploy somewhere you can reach from the tablet.** Tailscale + a home server, or a cheap VPS. Get the PWA installed on the tablet.

At the end of phase 1, start using it. Add the feeds you actually care about. See what's annoying.

### Phase 2: Make it actually pleasant (target: 2–3 more weekends)

Goal: the things that make this reader different from a generic one start showing up.

6. **Categories and per-category views.** Sources organized into user-defined categories. Per-category default view.
7. **Hide unread counts.** Implement the design doc's no-unread-counts default. This is partly a UI decision and partly about which data the API returns.
8. **Mark all as read everywhere.** Source-level, category-level, global. One tap, no confirmation.
9. **Pagination, not infinite scroll.** Clear "end of new items" state. Explicit action to load older items.
10. **Save for later.** Either a built-in flag (`saved: true` on items) with a "Saved" view, or integration with an external service. Built-in is faster to ship.

### Phase 3: YouTube (target: 1–2 weekends)

Goal: YouTube channels work, video playback is constrained.

11. **YouTube channel support.** Adding a channel by URL — resolve to channel ID, store the feed URL. The resolution step is the trickiest part.
12. **Video item rendering.** Title, channel, publication date, description. Optional thumbnails (off by default). No view counts, no engagement metrics.
13. **Iframe-based playback.** Tap a video, see detail view, tap play, video plays in iframe with `autoplay=0`, `rel=0`. When video ends (via IFrame Player API), return to reader automatically.

### Phase 4: Session management and limits (target: 2 weekends)

Goal: the friction layer the design doc describes.

14. **Session detection.** Track when the app is opened, how long it's been since the last session. Show the session-start prompt for first opens after a gap.
15. **Session timer and gentle interrupts.** Visible elapsed time, full-screen prompt at the 20-minute mark.
16. **Time limits for YouTube content.** Per-day budgets, soft warnings, hard block when exceeded. Changes to limits take effect tomorrow.
17. **Session end summary.** What was read, what was saved, time spent.

### Phase 5: Quality of life (ongoing)

Things that are nice but not essential:

18. **Full-text extraction for articles.** Readability.NET on the backend, cache extracted content per item.
19. **Subscription review.** Quarterly prompt showing low-engagement sources with one-tap unsubscribe.
20. **Reddit support.** If and when you're ready to add it back.
21. **Search.** Probably scoped to your own saved items, not the whole reader.
22. **Export.** OPML export so you're not locked in to your own tool.

---

## What to Defer or Skip

A few things that look tempting but probably aren't worth building for a personal tool:

- **Multi-user support.** You're one user. Don't build a user management system.
- **Real-time updates via WebSockets.** Pull-to-refresh and polling is fine.
- **Mobile native app.** The PWA is the point.
- **Cross-device sync.** The whole design assumes one device. Skip until you actually want it.
- **Recommendation features of any kind.** Not just deprioritized — explicitly out of scope.
- **Plugins or extensibility.** It's your tool, you can just add the feature directly.

---

## Risks and Things to Watch

A few things that could derail this project:

- **Scope creep into "real product."** This is a tool for you. Resist the urge to build features for hypothetical other users. If a feature isn't something you'll use in the den chair, it doesn't ship.
- **Building the system instead of using it.** Easy trap for developers. Set a deadline to start using phase 1 even if it's rough. Real usage will tell you what to build next better than planning will.
- **Letting the project become the new procrastination.** A custom reader you're "still working on" can become a reason to keep using YouTube and Reddit in the meantime. Consider using an off-the-shelf reader (Feedbin, Inoreader, NetNewsWire) for the first month while you build, so the behavior change starts immediately.
- **YouTube API or Reddit API changes.** Both platforms have been making programmatic access harder. Build the abstraction so a YouTube-feed source could be swapped for a different fetching mechanism without rewriting the rest.

---

## Revisit Date

Like the broader plan, check in on this roadmap 4–6 weeks after starting. Things to evaluate:

- Is the reader being used, or has it become a side project that doesn't get touched?
- Which planned features matter and which don't?
- What real-world friction has come up that the design didn't anticipate?
- Is the time-on-tool worth the time-saved-from-algorithmic-consumption?
