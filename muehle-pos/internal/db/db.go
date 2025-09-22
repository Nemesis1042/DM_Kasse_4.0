package db

import (
	"database/sql"
	_ "modernc.org/sqlite"
)

func Open(path string) (*sql.DB, error) {
	d, err := sql.Open("sqlite", path+"?_pragma=busy_timeout(5000)&_pragma=journal_mode(WAL)")
	if err != nil {
		return nil, err
	}
	if err := migrate(d); err != nil {
		return nil, err
	}
	return d, nil
}

func migrate(db *sql.DB) error {
	schema := `
CREATE TABLE IF NOT EXISTS event(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  active INTEGER DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS products(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  price_cents INTEGER NOT NULL,
  tax_rate_bp INTEGER NOT NULL,
  category TEXT,
  active INTEGER DEFAULT 1,
  color_hex TEXT,
  symbol TEXT,
  is_deposit INTEGER DEFAULT 0,
  event_id INTEGER,
  FOREIGN KEY(event_id) REFERENCES event(id)
);

CREATE TABLE IF NOT EXISTS orders(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  total_cents INTEGER NOT NULL,
  payment_method TEXT NOT NULL,
  event_id INTEGER,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY(event_id) REFERENCES event(id)
);

CREATE TABLE IF NOT EXISTS order_items(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  order_id INTEGER NOT NULL,
  product_id INTEGER,
  name TEXT NOT NULL,
  unit_cents INTEGER NOT NULL,
  qty INTEGER NOT NULL,
  tax_rate_bp INTEGER NOT NULL,
  line_cents INTEGER NOT NULL,
  FOREIGN KEY(order_id) REFERENCES orders(id),
  FOREIGN KEY(product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS pfand_refund(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  qty INTEGER NOT NULL,
  amount_cents INTEGER NOT NULL,
  event_id INTEGER,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS order_reversals(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  original_order_id INTEGER NOT NULL,
  reversal_order_id INTEGER NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(original_order_id),
  FOREIGN KEY(original_order_id) REFERENCES orders(id),
  FOREIGN KEY(reversal_order_id) REFERENCES orders(id)
);

CREATE TABLE IF NOT EXISTS settings(
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS app_kv(
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL
);

-- Defaults
INSERT INTO settings(key,value) VALUES
  ('deposit_cents','50'),
  ('printer_backend','test'),        -- test|auto|cups:<q>|/dev/usb/lp0|usb|usb:VID:PID|winspool:<name>
  ('receipt_width','32')
ON CONFLICT(key) DO NOTHING;

-- Ensure one active event
INSERT INTO event(name,active)
SELECT 'Standard',1 WHERE NOT EXISTS(SELECT 1 FROM event WHERE active=1);
`
	_, err := db.Exec(schema)
	return err
}
