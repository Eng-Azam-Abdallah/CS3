using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RofaqaaApi.Models;

public class GroupMember
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public int GroupId { get; set; }

    [Required]
    public GroupRole Role { get; set; } = GroupRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // العلاقات
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("GroupId")]
    public Group Group { get; set; } = null!;

    public ICollection<ExpenseShare> ExpenseShares { get; set; } = new List<ExpenseShare>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum GroupRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}