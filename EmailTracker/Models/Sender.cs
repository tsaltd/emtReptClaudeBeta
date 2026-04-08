using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailTracker.Models;

[Table("sender")]
public class Sender
{
    [Key]
    [Column("sender_id")]
    public int SenderId { get; set; }

    [Required]
    [Column("email_address")]
    [MaxLength(255)]
    public string EmailAddress { get; set; } = string.Empty;

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("first_seen")]
    public string? FirstSeen { get; set; }

    [Column("last_seen")]
    public string? LastSeen { get; set; }

    [Column("msg_count")]
    public int MsgCount { get; set; } = 0;

    [Column("rating_id")]
    public int RatingId { get; set; } = 5; // Default STEEL

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [Column("status_id")]
    public int StatusId { get; set; } = 1; // Default OPEN

    // Navigation
    [ForeignKey("RatingId")]
    public Rating? Rating { get; set; }

    [ForeignKey("StatusId")]
    public SenderStatus? Status { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
