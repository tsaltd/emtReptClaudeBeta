using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailTracker.Models;

[Table("priority_message")]
public class PriorityMessage
{
    [Key]
    [Column("record_id")]
    public int RecordId { get; set; }

    [Required]
    [Column("gmail_message_id")]
    public string GmailMessageId { get; set; } = string.Empty;
}
