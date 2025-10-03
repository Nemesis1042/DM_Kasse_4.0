using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashApp.Models
{
    public enum ProductCategory
    {
        Getränke = 0,
        Essen = 1,
        Pfand = 2,
        Sonstiges = 3
    }

    [Table("products")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 19.00m; // Standard MwSt 19%

        [Column(TypeName = "decimal(10,2)")]
        public decimal? DepositAmount { get; set; } // Pfandbetrag

        [Required]
        public ProductCategory Category { get; set; } = ProductCategory.Sonstiges;

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public bool IsActive { get; set; } = true;

        public bool RequiresDeposit { get; set; } = false;

        public int StockQuantity { get; set; } = 0;

        public int MinStockLevel { get; set; } = 0;

        [MaxLength(255)]
        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Calculated properties
        [NotMapped]
        public decimal PriceWithTax => Price * (1 + (TaxRate / 100));

        [NotMapped]
        public decimal TaxAmount => Price * (TaxRate / 100);

        [NotMapped]
        public bool IsLowStock => StockQuantity <= MinStockLevel && MinStockLevel > 0;

        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"{Name} - {Price:C}";
        }
    }
}
