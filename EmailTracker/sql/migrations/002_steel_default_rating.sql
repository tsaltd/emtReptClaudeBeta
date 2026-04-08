-- ============================================================
--  Migration 002 — STEEL as default rating
--  Move all PURPLE (rating_id=3) senders to STEEL (rating_id=5)
-- ============================================================

UPDATE sender SET rating_id = 5 WHERE rating_id = 3;
