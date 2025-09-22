package logic

import (
	"context"
	"database/sql"
	"time"
)

func RefundDeposit(ctx context.Context, db *sql.DB, qty int64, amountCents int64) error {
	_, err := db.ExecContext(ctx,
		"INSERT INTO pfand_refund(qty,amount_cents,event_id,created_at) VALUES(?,?,(SELECT id FROM event WHERE active=1 LIMIT 1),?)",
		qty, amountCents, time.Now())
	return err
}
