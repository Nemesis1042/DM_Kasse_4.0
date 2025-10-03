using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashApp.Models
{
    [Table("order_items")]
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; } // Preis zum Zeitpunkt der Bestellung

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } // MwSt zum Zeitpunkt der Bestellung

        [Column(TypeName = "decimal(10,2)")]
        public decimal? DepositAmount { get; set; } // Pfand pro Stück

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0; // Rabatt pro Stück

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        // Calculated properties
        [NotMapped]
        public decimal Subtotal => Quantity * UnitPrice;

        [NotMapped]
        public decimal TaxAmount => Quantity * (UnitPrice * (TaxRate / 100));

        [NotMapped]
        public decimal TotalTaxAmount => TaxAmount - (DiscountAmount * (TaxRate / 100));

        [NotMapped]
        public decimal DepositTotal => DepositAmount.HasValue ? Quantity * DepositAmount.Value : 0;

        [NotMapped]
        public decimal DiscountTotal => Quantity * DiscountAmount;

        [NotMapped]
        public decimal TotalAmount => Subtotal + TaxAmount + DepositTotal - DiscountTotal;

        [NotMapped]
        public decimal PriceWithTax => UnitPrice * (1 + (TaxRate / 100));

        [NotMapped]
        public decimal FinalUnitPrice => UnitPrice - DiscountAmount;

        [NotMapped]
        public decimal FinalTotalPrice => TotalAmount;

        public void CalculateFromProduct(Product product)
        {
            UnitPrice = product.Price;
            TaxRate = product.TaxRate;
            DepositAmount = product.RequiresDeposit ? product.DepositAmount : null;
        }

        public void ApplyDiscount(decimal discountAmount)
        {
            DiscountAmount = discountAmount;
        }

        public void ApplyPercentageDiscount(decimal percentage)
        {
            DiscountAmount = UnitPrice * (percentage / 100);
        }

        public override string ToString()
        {
            return $"{Quantity}x {Product.Name} - {TotalAmount:C}";
        }
    }
}
