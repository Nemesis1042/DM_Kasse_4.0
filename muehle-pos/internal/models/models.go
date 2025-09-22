package models

type Product struct {
	ID         int64
	Name       string
	PriceCents int64
	TaxRateBP  int64
	Category   string
	Active     bool
	ColorHex   string
	Symbol     string
	IsDeposit  bool
	EventID    *int64
}

type Order struct {
	ID            int64
	TotalCents    int64
	PaymentMethod string
	EventID       *int64
	CreatedAt     string
}

type OrderItem struct {
	ID         int64
	OrderID    int64
	ProductID  int64
	Name       string
	UnitCents  int64
	Qty        int64
	TaxRateBP  int64
	LineCents  int64
}

type Setting struct {
	Key   string
	Value string
}
