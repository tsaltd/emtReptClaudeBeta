-- Sender subset lists (named groups of senders)
CREATE TABLE IF NOT EXISTS sender_subset (
    subset_id    INTEGER PRIMARY KEY,
    subset_name  TEXT NOT NULL UNIQUE,
    created_at   TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at   TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS sender_subset_member (
    subset_id    INTEGER NOT NULL,
    sender_id    INTEGER NOT NULL,
    created_at   TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (subset_id, sender_id),
    FOREIGN KEY (subset_id) REFERENCES sender_subset(subset_id) ON DELETE CASCADE,
    FOREIGN KEY (sender_id) REFERENCES sender(sender_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_sender_subset_member_sender_id
    ON sender_subset_member(sender_id);
