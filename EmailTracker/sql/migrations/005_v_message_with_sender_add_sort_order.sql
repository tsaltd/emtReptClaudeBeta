-- Add sort_order to v_message_with_sender so message queries can be ordered by rating rank.
-- Required by VMessageWithSender.SortOrder property added to support rating-sorted message views.

DROP VIEW IF EXISTS v_message_with_sender;

CREATE VIEW v_message_with_sender AS
SELECT
  m.message_id,
  m.run_id,
  m.sender_id,
  s.email_address,
  r.rating_name,
  r.sort_order,
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
