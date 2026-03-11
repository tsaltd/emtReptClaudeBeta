using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EmailTracker.Models;

/// <summary>
/// Maps to SQLite view: v_sender_with_rating
/// </summary>
[Keyless]
public class VSenderWithRating
{
    [Column("sender_id")]        public int SenderId { get; set; }
    [Column("email_address")]    public string EmailAddress { get; set; } = string.Empty;
    [Column("display_name")]     public string? DisplayName { get; set; }
    [Column("first_seen")]       public string? FirstSeen { get; set; }
    [Column("last_seen")]        public string? LastSeen { get; set; }
    [Column("msg_count")]        public int MsgCount { get; set; }
    [Column("rating_id")]        public int RatingId { get; set; }
    [Column("rating_name")]      public string RatingName { get; set; } = string.Empty;
    [Column("created_at")]       public string CreatedAt { get; set; } = string.Empty;
    [Column("updated_at")]       public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Maps to SQLite view: v_message_with_sender
/// </summary>
[Keyless]
public class VMessageWithSender
{
    [Column("message_id")]       public int MessageId { get; set; }
    [Column("run_id")]           public int RunId { get; set; }
    [Column("sender_id")]        public int SenderId { get; set; }
    [Column("email_address")]    public string EmailAddress { get; set; } = string.Empty;
    [Column("rating_name")]      public string RatingName { get; set; } = string.Empty;
    [Column("gmail_message_id")] public string? GmailMessageId { get; set; }
    [Column("thread_id")]        public string? ThreadId { get; set; }
    [Column("internal_date")]    public string? InternalDate { get; set; }
    [Column("header_date")]      public string? HeaderDate { get; set; }
    [Column("subject")]          public string? Subject { get; set; }
    [Column("snippet")]          public string? Snippet { get; set; }
    [Column("from_raw")]         public string? FromRaw { get; set; }
    [Column("to_raw")]           public string? ToRaw { get; set; }
    [Column("created_at")]       public string CreatedAt { get; set; } = string.Empty;
}
