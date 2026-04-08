using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailTracker.Models;

[Table("sender_status")]
public class SenderStatus
{
    [Key]
    [Column("status_id")]
    public int StatusId { get; set; }

    [Required]
    [Column("status_name")]
    public string StatusName { get; set; } = string.Empty;
}
