using System.ComponentModel.DataAnnotations.Schema;

namespace EmailTracker.Models;

[Table("sender_subset_member")]
public class SenderSubsetMember
{
    [Column("subset_id")]
    public int SubsetId { get; set; }

    [Column("sender_id")]
    public int SenderId { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    public SenderSubset? Subset { get; set; }
    public Sender? Sender { get; set; }
}
