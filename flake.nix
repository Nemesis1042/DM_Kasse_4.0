{
  description = "CashApp Avalonia DevShell";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-24.05";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };
      in {
        devShells.default = pkgs.mkShell {
          buildInputs = with pkgs; [
            # .NET SDK
            dotnet-sdk_8

            # Avalonia Abhängigkeiten (X11/OpenGL für Linux)
            mesa
            libGL
            xorg.libX11
            xorg.libXcursor
            xorg.libXrandr
            xorg.libXext
            xorg.libXi

            # ICU (Unicode Support, oft benötigt von .NET)
            icu

            # optionale Tools
            git
            pkg-config
          ];

          shellHook = ''
            echo "🚀 CashApp DevShell gestartet"
            echo "dotnet --version => $(dotnet --version)"
            echo "Verwende 'dotnet build' oder 'dotnet publish' um dein Avalonia-Projekt zu bauen."
          '';
        };
      });
}

