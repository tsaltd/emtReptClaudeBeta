using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailTracker.Models;

[Table("sender_subset")]
public class SenderSubset
{
    [Key]
    [Column("subset_id")]
    public int SubsetId { get; set; }

    [Required]
    [Column("subset_name")]
    [MaxLength(120)]
    public string SubsetName { get; set; } = string.Empty;

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    public ICollection<SenderSubsetMember> Members { get; set; } = new List<SenderSubsetMember>();
}
