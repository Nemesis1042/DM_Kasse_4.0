package main

import (
	"log"

	"fyne.io/fyne/v2/app"
	"muehle-pos/internal/db"
	"muehle-pos/internal/ui"
)

func main() {
	database, err := db.Open("cashapp.db")
	if err != nil {
		log.Fatal(err)
	}
	defer database.Close()

	a := app.NewWithID("muehle-pos")
	ui.RunApp(a, database)
}
