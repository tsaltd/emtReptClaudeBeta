# Dashboard — Global Filter State & Multi-Tab Launch
**Prepared:** 2026-04-10
**Status:** Design decided — not yet implemented (next gen feature)

---

## Concept

The Dashboard acts as a persistent desktop control panel. Filter criteria set there
propagate to every view launched from it, and persist as the user navigates between forms.

---

## Global Filter State

- **Storage:** `localStorage` — already used in the app for sidebar and reading mode state
- **Scope:** All filter values (search term, rating, status, date from/to, priority flag)
- **Behavior:** Any filter change on **any form** writes back to localStorage immediately
- **On page load:** Every form reads localStorage and pre-populates its own FilterBar
- **Reset:** Clearing filters on any form clears localStorage — all subsequent views open with defaults

---

## Shift+Click — Isolated Tab Launch

- **Shift+Click** on a Dashboard launch button opens the view in a new tab
- The URL contains the full filter state as query string parameters (already implemented via `launch()`)
- An isolated tab **reads from URL params first**, falls back to localStorage only when params are absent
- An isolated tab **does not write back** to localStorage — it keeps its own state independently
- This allows multiple tabs with different filter sets to coexist simultaneously

---

## Two Modes Summary

| Mode | How triggered | Reads from | Writes to localStorage |
|---|---|---|---|
| **Global** | Normal click (current tab navigation) | localStorage | Yes — any change updates global |
| **Pinned/Isolated** | Shift+Click (new tab) | URL params | No — isolated from global state |

---

## Key Design Decision

> If a filter is changed on a form (not the dashboard), it updates the global state
> for all subsequent navigation. The dashboard is not the sole point of control —
> any form can update the global filter.

---

## Implementation Notes (for when this is built)

1. **`_FilterBar` partial** — on load, check URL params first, then localStorage; on change, write to localStorage (unless in isolated/URL-param mode)
2. **`launch()` function** — detect `event.shiftKey`; if true use `window.open(url)` instead of `window.location = url`
3. **Isolated tab detection** — if any filter param is present in the URL on load, treat tab as isolated (no localStorage writes)
4. **Reset** — clears both localStorage and current form FilterBar values
5. **Dashboard persistence** — Dashboard FilterBar pre-populates from localStorage on load so it always reflects current global state
