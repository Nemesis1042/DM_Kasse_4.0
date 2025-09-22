package ui

import (
	"database/sql"

	"fyne.io/fyne/v2"
	"fyne.io/fyne/v2/container"
)

func RunApp(a fyne.App, db *sql.DB) {
	w := a.NewWindow("Mühle Live POS")
	w.Resize(fyne.NewSize(1100, 700))

	tabs := BuildTabs(db)
	w.SetContent(container.NewMax(tabs))

	w.ShowAndRun()
}
