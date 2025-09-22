{
  description = "Mühle Live POS – Go + Fyne DevShell";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-24.05";

  outputs = { self, nixpkgs }: {
    devShells.x86_64-linux.default = let
      pkgs = import nixpkgs { system = "x86_64-linux"; };
    in pkgs.mkShell {
      buildInputs = [
        pkgs.go
        pkgs.pkg-config
        pkgs.libusb1
        pkgs.xorg.libX11
        pkgs.xorg.libXcursor
        pkgs.xorg.libXrandr
        pkgs.xorg.libXi
        pkgs.glfw
        pkgs.glew
      ];
      shellHook = ''
        echo "DevShell: Go/Fyne/sqlite/libusb bereit"
      '';
    };
  };
}
