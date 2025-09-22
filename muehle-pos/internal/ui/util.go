package ui

import (
	"database/sql"
	"fmt"
	"strconv"
	"strings"

	"fyne.io/fyne/v2"
	"fyne.io/fyne/v2/dialog"
)

func euro(cents int64) string {
	s := fmt.Sprintf("%.2f €", float64(cents)/100.0)
	return strings.ReplaceAll(s, ".", ",")
}

func parseEuroToCents(s string) int64 {
	s = strings.TrimSpace(strings.ReplaceAll(s, "€", ""))
	s = strings.ReplaceAll(s, ".", "")
	s = strings.ReplaceAll(s, ",", ".")
	f, _ := strconv.ParseFloat(s, 64)
	return int64(f * 100.0)
}

func getDepositCents(db *sql.DB) int64 {
	var v string
	_ = db.QueryRow("SELECT value FROM settings WHERE key='deposit_cents'").Scan(&v)
	c, _ := strconv.ParseInt(v, 10, 64)
	return c
}

func dialogNew(title string, content fyne.CanvasObject, onClose func(bool)) dialog.Dialog {
	d := dialog.NewCustomConfirm(title, "OK", "Abbrechen", content, onClose, nil)
	return d
}

func mustLoadProducts(db *sql.DB) []prodRow {
	rows, err := db.Query("SELECT id,name,price_cents,tax_rate_bp FROM products WHERE active=1 ORDER BY name")
	if err != nil {
		return nil
	}
	defer rows.Close()
	out := []prodRow{}
	for rows.Next() {
		var p prodRow
		rows.Scan(&p.id, &p.name, &p.price, &p.taxBP)
		out = append(out, p)
	}
	if len(out) == 0 {
		// Seed Beispielprodukte
        // 7% Essen, 19% Getränke
		db.Exec(`INSERT INTO products(name,price_cents,tax_rate_bp,category,active,is_deposit) VALUES
		 ('Pommes',350,700,'ESSEN',1,0),
		 ('Cola 0.5L',250,1900,'GETRÄNK',1,0),
		 ('Pfand',50,0,'SYSTEM',1,1)`)
		return mustLoadProducts(db)
	}
	return out
}

func printReceipt(db *sql.DB, orderID int64, total, given, change int64) {
	var backend string
	_ = db.QueryRow("SELECT value FROM settings WHERE key='printer_backend'").Scan(&backend)
	lines := []string{
		"Mühle Live POS",
		fmt.Sprintf("Order #%d", orderID),
		"-------------------------------",
		fmt.Sprintf("Summe: %s", euro(total)),
		fmt.Sprintf("Gegeben: %s", euro(given)),
		fmt.Sprintf("Wechselgeld: %s", euro(change)),
	}
	data := printing.BuildReceipt(lines, true)
	_ = printing.PrintRaw(printing.Config{Backend: backend}, data)
}
