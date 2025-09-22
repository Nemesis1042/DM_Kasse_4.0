package logic

import (
	"context"
	"database/sql"
	"errors"
	"time"
)

type CartLine struct {
	Name       string
	UnitCents  int64
	Qty        int64
	TaxRateBP  int64
	ProductID  int64
}

type SaleResult struct {
	OrderID    int64
	TotalCents int64
}

func ActiveEventID(db *sql.DB) (int64, error) {
	var id int64
	err := db.QueryRow("SELECT id FROM event WHERE active=1 LIMIT 1").Scan(&id)
	return id, err
}

func CreateSale(ctx context.Context, db *sql.DB, payment string, lines []CartLine) (SaleResult, error) {
	if len(lines) == 0 {
		return SaleResult{}, errors.New("empty cart")
	}

	tx, err := db.BeginTx(ctx, nil)
	if err != nil {
		return SaleResult{}, err
	}
	defer tx.Rollback()

	evID, err := ActiveEventID(db)
	if err != nil {
		return SaleResult{}, err
	}

	var total int64
	for _, l := range lines {
		total += l.UnitCents * l.Qty
	}

	res, err := tx.Exec(`INSERT INTO orders(total_cents,payment_method,event_id,created_at) VALUES(?,?,?,?)`,
		total, payment, evID, time.Now())
	if err != nil {
		return SaleResult{}, err
	}
	oid, _ := res.LastInsertId()

	for _, l := range lines {
		_, err = tx.Exec(`INSERT INTO order_items(order_id,product_id,name,unit_cents,qty,tax_rate_bp,line_cents)
			VALUES(?,?,?,?,?,?,?)`,
			oid, l.ProductID, l.Name, l.UnitCents, l.Qty, l.TaxRateBP, l.
