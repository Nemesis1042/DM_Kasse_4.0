using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashApp.Models
{
    public enum UserRole
    {
        Admin = 0,
        Kassierer = 1,
        Mitarbeiter = 2
    }

    [Table("users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty; // Gespeichert als Hash

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Mitarbeiter;

        [MaxLength(255)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        // Calculated properties
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [NotMapped]
        public bool IsAdmin => Role == UserRole.Admin;

        [NotMapped]
        public bool CanManageProducts => Role == UserRole.Admin || Role == UserRole.Kassierer;

        [NotMapped]
        public bool CanViewStatistics => Role == UserRole.Admin || Role == UserRole.Kassierer;

        [NotMapped]
        public bool CanManageUsers => Role == UserRole.Admin;

        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"{Username} ({FullName})";
        }
    }
}
