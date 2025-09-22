package ui

import (
	"context"
	"database/sql"
	"fmt"
	"strconv"

	"fyne.io/fyne/v2"
	"fyne.io/fyne/v2/container"
	"fyne.io/fyne/v2/layout"
	"fyne.io/fyne/v2/widget"
	"muehle-pos/internal/logic"
	"muehle-pos/internal/printing"
)

type prodRow struct {
	id    int64
	name  string
	price int64
	taxBP int64
}

func BuildTabs(db *sql.DB) *container.AppTabs {
	return container.NewAppTabs(
		container.NewTabItem("Verkauf", saleTab(db)),
		container.NewTabItem("Pfand", depositTab(db)),
		container.NewTabItem("Storno", widget.NewLabel("Storno: TODO")),
		container.NewTabItem("Produkte", widget.NewLabel("Produkte: TODO CRUD")),
		container.NewTabItem("Einstellungen", settingsTab(db)),
		container.NewTabItem("Statistik", widget.NewLabel("Statistik: TODO")),
	)
}

func saleTab(db *sql.DB) fyne.CanvasObject {
	products := mustLoadProducts(db)

	cart := []logic.CartLine{}
	cartList := widget.NewList(
		func() int { return len(cart) },
		func() fyne.CanvasObject { return container.NewHBox(widget.NewLabel("x"), layout.NewSpacer(), widget.NewLabel("€")) },
		func(i widget.ListItemID, o fyne.CanvasObject) {
			row := cart[i]
			h := o.(*fyne.Container)
			h.Objects[0].(*widget.Label).SetText(fmt.Sprintf("%dx %s", row.Qty, row.Name))
			h.Objects[2].(*widget.Label).SetText(euro(row.UnitCents*row.Qty))
		},
	)
	totalLbl := widget.NewLabel("0,00 €")

	addToCart := func(p prodRow) {
		found := false
		for i := range cart {
			if cart[i].ProductID == p.id && cart[i].UnitCents == p.price {
				cart[i].Qty++
				found = true
				break
			}
		}
		if !found {
			cart = append(cart, logic.CartLine{
				Name:      p.name,
				UnitCents: p.price,
				Qty:       1,
				TaxRateBP: p.taxBP,
				ProductID: p.id,
			})
		}
		cartList.Refresh()
		totalLbl.SetText(euro(sumCart(cart)))
	}

	grid := container.NewGridWrap(fyne.NewSize(180, 60))
	for _, p := range products {
		p := p
		btn := widget.NewButton(fmt.Sprintf("%s\n%s", p.name, euro(p.price)), func() { addToCart(p) })
		grid.Add(btn)
	}

	payCashBtn := widget.NewButton("Bar bezahlen", func() {
		total := sumCart(cart)
		if total == 0 {
			return
		}
		entry := widget.NewEntry()
		entry.SetPlaceHolder("Gegeben in €")
		d := dialogNew("Barzahlung", container.NewVBox(widget.NewLabel("Gesamt: "+euro(total)), entry), func(ok bool) {
			if !ok {
				return
			}
			givenCents := parseEuroToCents(entry.Text)
			change := givenCents - total
			res, err := logic.CreateSale(context.Background(), db, "BAR", cart)
			if err == nil {
				printReceipt(db, res.OrderID, total, givenCents, change)
				cart = []logic.CartLine{}
				cartList.Refresh()
				totalLbl.SetText("0,00 €")
			}
		})
		d.Show()
	})

	payCardBtn := widget.NewButton("Karte bezahlen", func() {
		total := sumCart(cart)
		if total == 0 {
			return
		}
		res, err := logic.CreateSale(context.Background(), db, "KARTE", cart)
		if err == nil {
			printReceipt(db, res.OrderID, total, total, 0)
			cart = []logic.CartLine{}
			cartList.Refresh()
			totalLbl.SetText("0,00 €")
		}
	})

	right := container.NewBorder(nil, container.NewHBox(widget.NewLabel("Summe:"), totalLbl, layout.NewSpacer(), payCashBtn, payCardBtn), nil, nil, cartList)
	content := container.NewHSplit(grid, right)
	content.SetOffset(0.6)
	return content
}

func depositTab(db *sql.DB) fyne.CanvasObject {
	qtyEntry := widget.NewEntry()
	qtyEntry.SetPlaceHolder("Anzahl Flaschen")
	info := widget.NewLabel("")
	submit := widget.NewButton("Auszahlen", func() {
		qty, _ := strconv.ParseInt(qtyEntry.Text, 10, 64)
		if qty <= 0 {
			info.SetText("Anzahl > 0")
			return
		}
		dep := getDepositCents(db)
		_ = logic.RefundDeposit(context.Background(), db, qty, dep*qty)
		info.SetText(fmt.Sprintf("Ausgezahlt: %s", euro(dep*qty)))
	})
	return container.NewVBox(widget.NewLabel("Pfand-Rückgabe"), qtyEntry, submit, info)
}

func settingsTab(db *sql.DB) fyne.CanvasObject {
	beSel := widget.NewEntry()
	beSel.SetPlaceHolder("printer backend, z.B. test or cups:EPSON")
	save := widget.NewButton("Speichern", func() {
		db.Exec("INSERT INTO settings(key,value) VALUES('printer_backend',?) ON CONFLICT(key) DO UPDATE SET value=excluded.value", beSel.Text)
	})
	test := widget.NewButton("Testdruck", func() {
		var backend string
		_ = db.QueryRow("SELECT value FROM settings WHERE key='printer_backend'").Scan(&backend)
		data := printing.BuildReceipt([]string{"Mühle Live POS", "Testdruck €"}, true)
		_ = printing.PrintRaw(printing.Config{Backend: backend}, data)
	})
	return container.NewVBox(widget.NewLabel("Drucker"), beSel, container.NewHBox(save, test))
}
