using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailTracker.Models;

[Table("run")]
public class Run
{
    [Key]
    [Column("run_id")]
    public int RunId { get; set; }

    [Required]
    [Column("window_start")]
    public string WindowStart { get; set; } = string.Empty;

    [Required]
    [Column("window_end")]
    public string WindowEnd { get; set; } = string.Empty;

    [Required]
    [Column("started_at")]
    public string StartedAt { get; set; } = string.Empty;

    [Column("source_label")]
    public string? SourceLabel { get; set; }

    // Navigation
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
