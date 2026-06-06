# Personal RSS Reader — UX Design Document

## Purpose

This is a personal RSS reader designed to support intentional information consumption. It is not a general-purpose feed aggregator. The product exists to solve a specific problem: replacing algorithmic, recommendation-driven consumption (YouTube, Reddit) with a curated, user-controlled alternative.

Every design decision should be evaluated against this question: **does this make consumption more intentional, or does it make consumption easier?** We want the former, even when it's in tension with conventional "good UX."

## Design Principles

### 1. Curation over completion

The reader is not an inbox. There is no expectation that the user reads everything. Sources that are consistently skipped should be easy to unsubscribe from, and the UI should not create pressure to clear unread items.

### 2. Friction in the right places

Conventional readers minimize friction everywhere. This reader deliberately adds friction at the points where unintentional consumption typically starts — opening a video, drifting from one source into another, extending a session past its intended end.

### 3. Sessions, not ambient use

The reader is designed for discrete reading sessions in a specific place (the den chair). It is not designed for quick checks throughout the day. The UI should reinforce this — opening the app feels like sitting down with a book, not like glancing at a notification.

### 4. The user is the algorithm

There is no recommendation engine, no "for you" feed, no trending section, no related items. Content reaches the user because the user subscribed to its source. Full stop.

---

## Core User Experience

### Source organization

Sources are organized into user-defined categories (e.g., "News," "Tech," "YouTube channels," "Reddit communities"). The default view is **per-category**, not a unified inbox. A unified view exists but is opt-in per session, not the default.

Rationale: a single chronological "everything" feed encourages clearing it like email. Per-category streams encourage dipping in to specific areas the user is currently interested in.

### No unread counts by default

Unread counts are hidden by default at every level — source, category, and global. The user can toggle them on per-source if a specific source genuinely benefits from it (e.g., a low-volume newsletter where missing an item matters), but the default is off.

Rationale: visible unread counts trigger completionist behavior, which is exactly the pattern this reader is designed to break.

### Mark-all-as-read is a first-class action

"Mark all as read" is available at every level (source, category, global) as a single tap with no confirmation dialog. There is no "are you sure?" The framing in the UI should treat this as a normal, healthy action — not a destructive one.

Rationale: if the user didn't get to it, it wasn't important. The reader should make it easy to acknowledge that and move on, not guilt the user into catching up.

### No infinite scroll

Within a source or category, items are paginated or have a clear end. When the user reaches the end of new items, the UI says so explicitly and does not load older content automatically. Loading older items requires an explicit action.

Rationale: infinite scroll is the canonical pattern for unintentional consumption. Breaking it is essential.

### Triage vs. deep reading are separated

The reader is for triage — scanning what's new, deciding what's worth attention. Items the user wants to actually read in depth are sent to a separate "read later" location (either an integrated read-later view or an external service like Readwise Reader, configurable by the user).

The reader should make "save for later" a prominent action, and should not encourage long reading sessions in the reader itself.

Rationale: mixing triage and deep reading in the same interface tends to mean neither happens well. Triage becomes browsing; deep reading gets interrupted by new items appearing.

---

## Session Management

### Session start

When the user opens the app after a meaningful gap (e.g., more than an hour), the UI presents a brief session-start state: "Welcome back. What are you here for?" with options like "Catching up on news," "Checking specific sources," or "Just browsing." This is not a hard gate — it can be dismissed — but it exists to prompt intentionality.

The selected intent is shown subtly in the UI during the session as a reminder.

### Session timer

A subtle, non-alarming timer is visible during the session showing elapsed time. It is not a countdown and does not interrupt — it's just present, the way a clock on the wall is present.

At configurable thresholds (default: 20 minutes), the UI shows a gentle interrupt: a full-screen card saying "You've been reading for 20 minutes. Keep going, or wrap up?" with two options. Choosing "wrap up" doesn't lock the app — it just changes the UI to show a session summary and encourages exit.

### Session end ritual

When the user chooses to end a session (or after a longer threshold like 45 minutes), the UI presents a session summary: what was read, what was saved for later, what was marked as read without opening. This serves as a natural close and also gives the user data to reflect on at review time.

### Daily and weekly limits

The reader supports per-source-type time limits (e.g., "1 hour of YouTube content per weekday, 2 on weekends"). When a limit is approached, the UI warns; when it's reached, content from that source type is hidden for the rest of the period. This is enforced by the reader for content routed through it; external enforcement (network-level blocking, OS screen time) handles content accessed outside the reader.

Limits are easy to view and easy to change — but changes take effect the next day, not immediately. The in-the-moment user cannot raise their own limit.

---

## YouTube Integration

YouTube channels are subscribed to via their RSS feeds (`youtube.com/feeds/videos.xml?channel_id=...`). The reader treats YouTube channels as a category of source, not a special integration.

### Video item presentation

Video items show: title, channel, duration, publication date, and the channel-provided description. They do **not** show: thumbnails by default (optional toggle), view counts, like counts, or any engagement metrics.

Rationale: thumbnails and engagement metrics are optimized to drive clicks, which is exactly what we're trying to avoid.

### Playing videos

Tapping a video item does not immediately play it. It opens a detail view with the full description and a single "Play" button. Playing the video opens an embedded player (or the YouTube app, configurable) with:

- Autoplay of the next video **disabled** (no end-screen autoplay, no related-video autoplay)
- End-screen suggestions hidden where possible
- No access to the YouTube homepage, sidebar, or search from within the playback flow

When the video ends, the user returns to the reader, not to YouTube.

### No browsing YouTube

The reader provides no way to browse YouTube. There is no search, no "find new channels," no related videos. Adding a new channel requires the user to find it elsewhere and explicitly add its RSS feed.

---

## Reddit Integration (Future)

Reddit subreddits are subscribed to via their RSS feeds (`reddit.com/r/subname/.rss`). Reddit support is **disabled by default** during the initial use period (the user is taking a break from Reddit), and can be enabled later.

When enabled, Reddit posts are shown as text items with title, author, subreddit, and post body. Comments are not shown in the reader — viewing comments requires clicking through to Reddit in a browser, which is intentional friction.

Reddit's RSS feeds have limits and Reddit has been increasingly hostile to programmatic access. The reader should degrade gracefully when feeds fail and should not be the user's primary Reddit interface.

---

## Subscription Management

### Adding sources

Adding a source requires a feed URL. There is no in-app discovery, no "popular feeds," no "you might like." If the user wants to find new sources, they do so outside the reader.

### Aggressive pruning

The reader tracks per-source engagement: how often items from this source are opened vs. skipped, how often they're saved for later vs. discarded. When a source has been consistently skipped (e.g., 80%+ skip rate over 30+ items), the reader surfaces this and suggests unsubscribing. This is shown as a quarterly "subscription review," not as a constant nag.

### Easy unsubscribe

Unsubscribing is one tap from any item, with no confirmation. The user can always re-add a source.

---

## What This Reader Deliberately Does Not Have

- Recommendation engine
- Trending or popular sections
- Social features (sharing, comments, following other users)
- Notifications or badges
- Push notifications of any kind
- A homepage feed mixing all sources
- Infinite scroll
- Engagement metrics (view counts, like counts) for any content
- In-app search for new content
- Autoplay of any media

---

## Open Questions for Implementation

1. **Read-later integration vs. built-in.** Build a native read-later view, integrate with an existing service (Readwise Reader, Pocket, Instapaper), or both? Native is simpler but more work; integration is faster but adds a dependency.

2. **YouTube playback environment.** Embedded player inside the reader (more control, but technically harder and against YouTube ToS for some approaches) vs. handing off to the YouTube app (less control, but legitimate). The web player with extensions like Unhook is a third option but only works in a browser context.

3. **Sync across devices.** Is this a single-device app (the den tablet) or does it sync? Single-device is simpler and reinforces the spatial separation principle. Sync is more flexible but undermines the "one place for consumption" idea.

4. **Data storage and privacy.** Local-only, self-hosted backend, or third-party sync service? Local-only is simplest and most private but loses data on device failure.

5. **Session limit enforcement strength.** When a limit is hit, is content hidden (soft block, can be dismissed) or genuinely blocked (hard block, requires changing settings tomorrow)? The hard block is more effective but might cause frustration in legitimate edge cases.

---

## Review and Iteration

This design should be revisited at the same review date set for the broader information-consumption plan (4–6 weeks after starting). Specifically, evaluate:

- Are the friction points in the right places, or are they just annoying?
- Is the session model actually being used, or is the reader becoming ambient again?
- Which sources are being read, and which should be cut?
- What patterns of unintentional consumption have emerged that the reader doesn't address?

The reader is a tool to support a behavior change, not a finished product. Expect to change it.
