using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashApp.Models
{
    public enum AuditLogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public enum AuditAction
    {
        // Authentication
        Login = 0,
        Logout = 1,
        LoginFailed = 2,

        // Orders
        OrderCreated = 100,
        OrderModified = 101,
        OrderPaid = 102,
        OrderCancelled = 103,
        OrderItemAdded = 104,
        OrderItemRemoved = 105,

        // Products
        ProductCreated = 200,
        ProductModified = 201,
        ProductDeleted = 202,
        ProductPriceChanged = 203,

        // Users
        UserCreated = 300,
        UserModified = 301,
        UserDeleted = 302,
        UserRoleChanged = 303,

        // System
        BackupCreated = 400,
        BackupRestored = 401,
        SettingsChanged = 402,
        DatabaseMaintenance = 403,

        // Pfand
        DepositReturned = 500,
        DepositRecorded = 501,

        // Statistics
        ReportGenerated = 600,
        ReportExported = 601,

        // Other
        Other = 999
    }

    [Table("logs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Wer hat die Aktion ausgeführt

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public AuditLogLevel Level { get; set; } = AuditLogLevel.Info;

        [Required]
        public AuditAction Action { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Details { get; set; } // Zusätzliche Details als JSON

        [MaxLength(255)]
        public string? EntityType { get; set; } // Z.B. "Product", "Order", "User"

        public int? EntityId { get; set; } // ID des betroffenen Objekts

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(1000)]
        public string? AdditionalData { get; set; } // Weitere Daten als JSON

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        // Helper methods
        public static AuditLog Create(int userId, AuditAction action, string message, AuditLogLevel level = AuditLogLevel.Info)
        {
            return new AuditLog
            {
                UserId = userId,
                Action = action,
                Message = message,
                Level = level,
                Timestamp = DateTime.UtcNow
            };
        }

        public AuditLog WithDetails(string details)
        {
            Details = details;
            return this;
        }

        public AuditLog WithEntity(string entityType, int entityId)
        {
            EntityType = entityType;
            EntityId = entityId;
            return this;
        }

        public AuditLog WithIpAddress(string ipAddress)
        {
            IpAddress = ipAddress;
            return this;
        }

        public AuditLog WithAdditionalData(string data)
        {
            AdditionalData = data;
            return this;
        }

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Action}: {Message}";
        }
    }
}
