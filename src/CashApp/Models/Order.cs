using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashApp.Models
{
    public enum PaymentMethod
    {
        Bar = 0,
        Karte = 1,
        Gutschein = 2,
        Sonstiges = 3
    }

    public enum OrderStatus
    {
        Offen = 0,
        Bezahlt = 1,
        Storniert = 2,
        TeilweiseStorniert = 3
    }

    [Table("orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Kassierer

        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty; // Eindeutige Bestellnummer

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Offen;

        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Bar;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; } = 0; // Summe ohne MwSt

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TaxAmount { get; set; } = 0; // MwSt Betrag

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; } = 0; // Gesamtsumme

        [Column(TypeName = "decimal(10,2)")]
        public decimal DepositTotal { get; set; } = 0; // Pfand Gesamt

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0; // Rabatt

        [Column(TypeName = "decimal(10,2)")]
        public decimal PaidAmount { get; set; } = 0; // Bezahlter Betrag

        [Column(TypeName = "decimal(10,2)")]
        public decimal ChangeAmount { get; set; } = 0; // Rückgeld

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsTestOrder { get; set; } = false; // Testmodus

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Calculated properties
        [NotMapped]
        public decimal RemainingAmount => TotalAmount - PaidAmount;

        [NotMapped]
        public bool IsPaid => Status == OrderStatus.Bezahlt;

        [NotMapped]
        public bool IsCancelled => Status == OrderStatus.Storniert;

        [NotMapped]
        public bool CanBeModified => Status == OrderStatus.Offen;

        [NotMapped]
        public TimeSpan? ProcessingTime => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : null;

        public void CalculateTotals()
        {
            Subtotal = OrderItems.Sum(item => item.Subtotal);
            TaxAmount = OrderItems.Sum(item => item.TaxAmount);
            DepositTotal = OrderItems.Sum(item => item.DepositAmount);
            TotalAmount = Subtotal + TaxAmount + DepositTotal - DiscountAmount;

            // Ensure TotalAmount is not negative
            if (TotalAmount < 0)
                TotalAmount = 0;
        }

        public void MarkAsPaid()
        {
            Status = OrderStatus.Bezahlt;
            CompletedAt = DateTime.UtcNow;
            PaidAmount = TotalAmount;
            ChangeAmount = 0;
        }

        public void MarkAsCancelled()
        {
            Status = OrderStatus.Storniert;
            CompletedAt = DateTime.UtcNow;
        }

        public void UpdateTimestamp()
        {
            // This could be used if we add an UpdatedAt field
        }

        public override string ToString()
        {
            return $"Bestellung {OrderNumber} - {TotalAmount:C}";
        }
    }
}
