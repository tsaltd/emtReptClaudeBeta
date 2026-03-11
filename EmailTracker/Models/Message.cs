using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailTracker.Models;

[Table("message")]
public class Message
{
    [Key]
    [Column("message_id")]
    public int MessageId { get; set; }

    [Required]
    [Column("run_id")]
    public int RunId { get; set; }

    [Required]
    [Column("sender_id")]
    public int SenderId { get; set; }

    [Column("gmail_message_id")]
    public string? GmailMessageId { get; set; }

    [Column("thread_id")]
    public string? ThreadId { get; set; }

    [Column("internal_date")]
    public string? InternalDate { get; set; }

    [Column("header_date")]
    public string? HeaderDate { get; set; }

    [Column("subject")]
    public string? Subject { get; set; }

    [Column("snippet")]
    public string? Snippet { get; set; }

    [Column("from_raw")]
    public string? FromRaw { get; set; }

    [Column("to_raw")]
    public string? ToRaw { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    // Navigation
    [ForeignKey("RunId")]
    public Run? Run { get; set; }

    [ForeignKey("SenderId")]
    public Sender? Sender { get; set; }
}
