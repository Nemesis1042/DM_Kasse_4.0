# Backlog – Mühle Live POS

## Epic 1: Verkauf & Warenkorb
- [ ] **Story 1.1**: Produktkacheln laden und Warenkorb befüllen (Menge +/−)
- [ ] **Story 1.2**: Zahlungsarten BAR/KARTE mit Wechselgeldberechnung
- [ ] **Story 1.3**: Buchung schreibt `orders` und `order_items`
- [ ] **Story 1.4**: Storno-Funktion erstellt Reversal-Order und Storno-Beleg

## Epic 2: Pfand
- [ ] **Story 2.1**: Pfandbetrag in `settings` speichern
- [ ] **Story 2.2**: Pro Getränke-Item Pfandbon drucken
- [ ] **Story 2.3**: Pfandrückgabe mit Auszahlung und `pfand_refund`-Eintrag

## Epic 3: Drucken
- [ ] **Story 3.1**: ESC/POS-Basissequenzen (Init, Bold, Cut)
- [ ] **Story 3.2**: CP1252-Encoding mit ESC t 16 („€“ korrekt)
- [ ] **Story 3.3**: Backends: `auto`, `/dev/usb/lp*`, `cups:<queue>`, `usb:VID:PID`, `winspool`
- [ ] **Story 3.4**: Drucker-UI mit Dropdown + Refresh
- [ ] **Story 3.5**: Testmodus: Konsolenausgabe statt Druck
- [ ] **Story 3.6**: Nachdruck-Schleife mit Bestätigungsdialog

## Epic 4: Verwaltung
- [ ] **Story 4.1**: Produkte CRUD inkl. MwSt-Sätze, Farben, Symbole, aktiv/inaktiv
- [ ] **Story 4.2**: Events CRUD mit Aktiv-Flag
- [ ] **Story 4.3**: Settings CRUD (Pfand, Drucker, Backup-Pfad, Kopf-/Fußtext)

## Epic 5: Statistik & Exporte
- [ ] **Story 5.1**: Zeitraumfilter (Heute, Gestern, 7/30 Tage, Monat)
- [ ] **Story 5.2**: Umsatz, Verkäufe, Top-Produkte, Zahlarten, Kategorien
- [ ] **Story 5.3**: CSV/PDF-Exporte (Statistik, Tagesabschluss, Finanzamt)
- [ ] **Story 5.4**: XLSX-Export mit Excelize

## Epic 6: Backups
- [ ] **Story 6.1**: Backup-Job beim Start mit Scheduler
- [ ] **Story 6.2**: Manuelles „Backup jetzt“-UI
- [ ] **Story 6.3**: Backups mit Datum im Dateinamen

## Epic 7: Deployment & Plattform
- [ ] **Story 7.1**: Windows-Build (.exe) mit Spooler
- [ ] **Story 7.2**: Linux-Build mit udev-Regeln für USB
- [ ] **Story 7.3**: NixOS-Flake + DevShell (go, pkg-config, libusb, X11/GL)
- [ ] **Story 7.4**: Docker/Compose-Variante

## Epic 8: Stabilität & Tests
- [ ] **Story 8.1**: Langzeittest: mehrere Stunden stabil, DB konsistent
- [ ] **Story 8.2**: Testfälle: Drucker offline, USB detach, große Warenkörbe
- [ ] **Story 8.3**: Validierung MwSt- und Pfandlogik

