using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailTracker.Models;

[Table("rating")]
public class Rating
{
    [Key]
    [Column("rating_id")]
    public int RatingId { get; set; }

    [Required]
    [Column("rating_name")]
    [MaxLength(100)]
    public string RatingName { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    // Navigation
    public ICollection<Sender> Senders { get; set; } = new List<Sender>();
}
