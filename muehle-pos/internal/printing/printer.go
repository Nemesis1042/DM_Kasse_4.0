package printing

import (
	"errors"
	"fmt"
	"os"
	"os/exec"
	"strings"
)

type Config struct {
	Backend string // test|auto|cups:<queue>|/dev/usb/lp0|usb|usb:VID:PID|winspool:<name>
}

func PrintRaw(cfg Config, data []byte) error {
	b := cfg.Backend
	switch {
	case b == "" || b == "test":
		fmt.Printf("[TEST PRINT]\n% X\n", data)
		return nil
	case strings.HasPrefix(b, "/dev/"):
		f, err := os.OpenFile(b, os.O_WRONLY, 0o666)
		if err != nil {
			return err
		}
		defer f.Close()
		_, err = f.Write(data)
		return err
	case strings.HasPrefix(b, "cups:"):
		q := strings.TrimPrefix(b, "cups:")
		cmd := exec.Command("lp", "-d", q, "-o", "raw")
		stdin, err := cmd.StdinPipe()
		if err != nil {
			return err
		}
		if err := cmd.Start(); err != nil {
			return err
		}
		if _, err := stdin.Write(data); err != nil {
			return err
		}
		_ = stdin.Close()
		return cmd.Wait()
	case strings.HasPrefix(b, "auto"):
		// Simple auto: try default CUPS without queue name
		cmd := exec.Command("lp", "-o", "raw")
		stdin, err := cmd.StdinPipe()
		if err != nil {
			return err
		}
		if err := cmd.Start(); err != nil {
			return err
		}
		if _, err := stdin.Write(data); err != nil {
			return err
		}
		_ = stdin.Close()
		return cmd.Wait()
	case strings.HasPrefix(b, "usb"):
		// Stub: direct libusb not implemented in this minimal base
		return errors.New("usb backend not implemented in base build; use CUPS or /dev/usb/lp*")
	case strings.HasPrefix(b, "winspool:"):
		return errors.New("winspool not implemented in base build")
	default:
		return fmt.Errorf("unknown backend: %s", b)
	}
}
