# EmailTracker Requirements

## Open

| # | Requirement | Source | Date | Notes |
|---|------------|--------|------|-------|
| 1 | Individual Gmail sender rating — CEA should support individual email addresses (e.g. john.doe@gmail.com) not just domain-level (gmail.com) | User observation | 2026-06-11 | Currently all @gmail.com senders share one rating. Requires upstream ingestion change. |
| 2 | "Promote to Sender" feature — from CeaReports view, allow user to promote an individual from_raw entry into its own Sender record using the parsed/cleaned email address as the CEA | User request | 2026-06-11 | CeaReports already displays individual from_raw groups under domain CEAs. Need UI action + backend logic to parse email from from_raw, create new Sender record, and reassign messages. |

## Completed

| # | Requirement | Date Completed | Notes |
|---|------------|----------------|-------|
