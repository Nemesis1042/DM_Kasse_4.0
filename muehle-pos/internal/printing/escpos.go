package printing

import (
	"bytes"

	"golang.org/x/text/encoding/charmap"
)

func escInit() []byte        { return []byte{0x1B, 0x40} }             // ESC @
func escSelectCP1252() []byte { return []byte{0x1B, 0x74, 16} }         // ESC t 16
func gsCut() []byte          { return []byte{0x1D, 0x56, 0x00} }       // GS V 0
func lf() []byte             { return []byte{0x0A} }

func cp1252Bytes(s string) []byte {
	b, _ := charmap.Windows1252.NewEncoder().Bytes([]byte(s))
	return b
}

// BuildReceipt returns ESC/POS bytes for a simple receipt.
func BuildReceipt(lines []string, doCut bool) []byte {
	var buf bytes.Buffer
	buf.Write(escInit())
	buf.Write(escSelectCP1252())
	for _, l := range lines {
		buf.Write(cp1252Bytes(l))
		buf.Write(lf())
	}
	if doCut {
		buf.Write(gsCut())
	}
	return buf.Bytes()
}
