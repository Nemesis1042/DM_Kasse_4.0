# Pflichtenheft – Kassensystem

## 1. Zielsetzung

Das System soll ein modernes, leicht bedienbares Kassensystem darstellen, das für Veranstaltungen und kleinere Betriebe geeignet ist. Es muss offlinefähig sein, aber optional auch Synchronisationsfunktionen unterstützen.

---

## 2. Plattform & Deployment

- Betrieb auf verschiedenen Betriebssystemen (z. B. Windows, Linux, macOS).
- Später optional auch Tablets/Touchgeräte.
- Deployment als eigenständige Anwendung (kein Server oder Browser erforderlich).

---

## 3. Datenhaltung

- Eingebettete Datenbank (lokal, portabel).
- Tabellen:
  - `products` (Produkte, Kategorien, Preise, MwSt, Pfandinformationen)
  - `orders` (Bestellungen mit Zeitstempel, Zahlart, Benutzer, Gesamtsumme)
  - `order_items` (Artikel pro Bestellung)
  - `users` (Benutzer, Rollen, Berechtigungen)
  - `logs` (Audit-Log)
- Persistente Speicherung über mehrere Tage/Wochen.
- Automatische Backup-Funktion (lokal und extern).

---

## 4. Benutzer & Sicherheit

- Login mit Benutzerrollen (z. B. Kassierer, Admin).
- Rechteverwaltung (nur Admins dürfen Produkte ändern oder Statistiken exportieren).
- Audit-Log: Alle Aktionen werden mit Benutzer und Zeit protokolliert.

---

## 5. Benutzeroberfläche

- Mehrere Tabs/Module:
  - **Kasse**: Verkaufsvorgang mit Warenkorb, Beträgen, Rückgeldanzeige.
  - **Produkte**: Verwaltung von Artikeln, Kategorien, Preisen, MwSt, Pfand.
  - **Pfand**: Übersicht offene Pfandbeträge, Rückgaben, automatische Verrechnung.
  - **Statistik**: Umsatz, Produkte, Kategorien, Zeitdiagramme.
  - **Einstellungen**: Event-Infos, Druckerwahl, Theme, Debug.
  - **Benutzerverwaltung**: Anlegen, Bearbeiten, Rechtevergabe.
- Touch-optimierte Oberfläche für Tablets.
- Mehrsprachigkeit (z. B. Deutsch/Englisch).
- Dark-/Light-Mode.

---

## 6. Kassenfunktionen

- Produkte per Button oder Suche hinzufügen.
- Mengenwahl vor oder nach Auswahl.
- Warenkorb mit Übersicht, Entfernen, Storno einzelner Artikel.
- Berechnung von Zwischensumme, MwSt und Gesamtsumme.
- Rabatte (Betrag oder Prozent).
- Zahlungsarten: bar, Karte, Gutschein (erweiterbar).
- Rückgeldberechnung mit Schnellbeträgen (z. B. 5/10/20/50/100).
- Bestätigung vor Abschluss.
- Tagesabschluss/Z-Bericht mit Kassensturz.
- Testmodus (Bestellungen ohne echte Speicherung/Druck).

---

## 7. Pfandverwaltung

- Kennzeichnung pfandpflichtiger Produkte.
- Automatische Pfandberechnung.
- Übersicht über offene und zurückgegebene Pfandbeträge.
- Automatische Verrechnung bei Rückgabe.

---

## 8. Drucker & Bons

- Automatische Erkennung angeschlossener Drucker (USB/Netzwerk).
- Konfigurierbares Bon-Design (Kopf-/Fußtext, Schriftgröße).
- Testdruck-Funktion.
- Druck von Einzelbons pro Bestellung oder Sammelbons.
- Druck von Tagesabschluss/Z-Berichten.

---

## 9. Statistik & Auswertung

- Umsatzübersicht (gesamt, pro Tag, pro Kassierer).
- Produktstatistik (verkaufte Mengen, Umsatz, MwSt).
- Kategorienstatistik (z. B. Getränke/Essen/Pfand).
- Zeitbasierte Analyse (täglich, wöchentlich, monatlich).
- Visualisierung durch Diagramme (Balken, Linien, Kreisdiagramme).
- Export: CSV und PDF.

---

## 10. System & Betrieb

- Offlinefähig, optional Synchronisierung mehrerer Kassen.
- Automatische Backups (lokal, extern).
- Debug-/Logging-System (Konsole, Datei, Level: DEBUG–CRITICAL).
- Plattformunabhängig durch portable Architektur.

---

## 11. Nicht-funktionale Anforderungen

- **Performance**: Schnelle Bedienung auch bei vielen Produkten und Bestellungen.
- **Usability**: Intuitive UI, optimiert für Touch und Tastatur.
- **Robustheit**: Fehlermeldungen mit klaren Hinweisen.
- **Sicherheit**: Zugriffsschutz durch Benutzerrollen und Audit-Logs.
