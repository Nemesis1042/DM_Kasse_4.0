{
  description = "CashApp Go + Fyne DevShell with Windows cross-compile";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-24.05";

  outputs = { self, nixpkgs }: {
    devShells.x86_64-linux.default =
      let
        pkgs = import nixpkgs { system = "x86_64-linux"; };
      in
      pkgs.mkShell {
        buildInputs = [
          # Go Toolchain
          pkgs.go
          pkgs.pkg-config
          pkgs.libusb1
          
          # Linux native build deps (Fyne/OpenGL/X11)
          pkgs.mesa
          pkgs.xorg.libX11
          pkgs.xorg.libXcursor
          pkgs.xorg.libXext
          pkgs.xorg.libXrandr
          pkgs.xorg.libXinerama
          pkgs.xorg.libXi
          pkgs.glfw
          pkgs.glew
          pkgs.xorg.xorgproto

          # Cross compile for Windows
          pkgs.pkgsCross.mingwW64.stdenv.cc
          pkgs.docker
        ];

        shellHook = ''
          echo "Go + Fyne build environment ready"
          echo
          echo "Linux build:"
          echo "  go build -o cashapp ./cmd/cashapp"
          echo
          echo "Windows cross-build:"
          echo "  GOOS=windows GOARCH=amd64 go build -o cashapp.exe ./cmd/cashapp"
          echo
        '';
      };
  };
}
