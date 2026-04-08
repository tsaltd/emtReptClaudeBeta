-- ============================================================
--  Migration 003 — Add sender_status lookup + status_id on sender
--  Run once against gmailanalysis.db
-- ============================================================

-- ── 1. Create lookup table ───────────────────────────────────────
CREATE TABLE IF NOT EXISTS sender_status (
    status_id   INTEGER PRIMARY KEY,
    status_name TEXT    NOT NULL
);
INSERT OR IGNORE INTO sender_status (status_id, status_name) VALUES (1, 'OPEN');
INSERT OR IGNORE INTO sender_status (status_id, status_name) VALUES (2, 'RATED');

-- ── 2. Add status_id column to sender ────────────────────────────
ALTER TABLE sender ADD COLUMN status_id INTEGER NOT NULL DEFAULT 1;

-- ── 3. Rebuild v_sender_with_rating (add sender_id + status) ─────
DROP VIEW IF EXISTS v_sender_with_rating;
CREATE VIEW v_sender_with_rating AS
SELECT
  s.sender_id,
  s.email_address,
  s.display_name,
  s.first_seen,
  s.last_seen,
  s.msg_count,
  s.rating_id,
  r.rating_name,
  r.color_code,
  s.status_id,
  ss.status_name,
  s.created_at,
  s.updated_at
FROM sender s
LEFT JOIN rating         r  ON s.rating_id  = r.rating_id
LEFT JOIN sender_status  ss ON s.status_id  = ss.status_id;

-- ── 4. Rebuild v_message_with_sender (add status) ─────────────────
DROP VIEW IF EXISTS v_message_rept_base;
DROP VIEW IF EXISTS v_message_with_sender;
CREATE VIEW v_message_with_sender AS
SELECT
  m.message_id,
  m.run_id,
  m.sender_id,
  s.email_address,
  r.rating_name,
  r.color_code,
  ss.status_name,
  m.gmail_message_id,
  m.thread_id,
  m.internal_date,
  m.header_date,
  m.subject,
  m.snippet,
  m.from_raw,
  m.to_raw,
  m.created_at
FROM message m
JOIN  sender         s  ON m.sender_id  = s.sender_id
LEFT JOIN rating     r  ON s.rating_id  = r.rating_id
LEFT JOIN sender_status ss ON s.status_id = ss.status_id;

-- ── 5. Recreate v_message_rept_base ──────────────────────────────
CREATE VIEW v_message_rept_base AS
SELECT v.email_address FROM v_message_with_sender v;

-- ── Verify ───────────────────────────────────────────────────────
-- SELECT sender_id, email_address, status_id, status_name FROM v_sender_with_rating LIMIT 5;
