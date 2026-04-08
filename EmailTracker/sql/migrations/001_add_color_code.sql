-- ============================================================
--  Migration 001 — Add color_code to rating + fix views
--  Run once against gmailanalysis.db
-- ============================================================

-- color_code already added to rating table:
-- ALTER TABLE rating ADD COLUMN color_code TEXT;
-- UPDATE rating SET color_code = '#28a745' WHERE rating_id = 1;  -- GREEN
-- UPDATE rating SET color_code = '#ffc107' WHERE rating_id = 2;  -- YELLOW
-- UPDATE rating SET color_code = '#fd7e14' WHERE rating_id = 3;  -- ORANGE
-- UPDATE rating SET color_code = '#dc3545' WHERE rating_id = 4;  -- RED

-- ── Fix v_sender_with_rating (was missing JOIN, add color_code) ──
DROP VIEW IF EXISTS v_sender_with_rating;
CREATE VIEW v_sender_with_rating AS
SELECT

  s.email_address,
  s.display_name,
  s.first_seen,
  s.last_seen,
  s.msg_count,
  s.rating_id,
  r.rating_name,
  r.color_code,
  s.created_at,
  s.updated_at
FROM sender s
LEFT JOIN rating r ON s.rating_id = r.rating_id;

-- ── Create v_message_with_sender (was never created) ─────────────
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
JOIN sender s ON m.sender_id = s.sender_id
LEFT JOIN rating r ON s.rating_id = r.rating_id;

-- ── Recreate v_message_rept_base (depends on v_message_with_sender)
CREATE VIEW v_message_rept_base AS
SELECT
    v.email_address
FROM v_message_with_sender v;
