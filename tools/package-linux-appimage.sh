#!/bin/bash
# Builds OpenNSPW-<version>-linux-x64.AppImage in the output directory: the desktop app for x64 Linux, self-contained in
# a single-file executable, as release.yml publishes it. Runs on x64 Linux only, with ImageMagick for the icon, and
# downloads appimagetool and the AppImage runtime, checked against their SHA-256.
#
# tools/package-linux-appimage.sh <version> <output directory>
#
# The AppImage finds the original's files (Original/) beside it.

set -euo pipefail

version=$1
output=$2
root=$(dirname "$0")/..
work=$(mktemp -d)
appdir=$work/OpenNSPW.AppDir

# The file at the URL, if its SHA-256 is the one given.
download() {
	curl --fail --location --silent --show-error --output "$work/$1" "$2"
	echo "$3  $work/$1" | sha256sum --check --quiet
}

# One executable, with only what it reads from its own directory beside it: libSDL2-2.0.so.0 and libopenal.so, which
# MonoGame loads from there, and Fonts.
dotnet publish "$root/OpenNSPW/OpenNspw.Desktop" --configuration Release --runtime linux-x64 --self-contained \
	/p:PublishSingleFile=true /p:DebugType=embedded /p:GenerateDocumentationFile=false /p:Version="$version" \
	--output "$appdir/usr/bin"

# The icon from the window's (Icon.bmp, the original's 32x32 icon).
convert "$root/OpenNSPW/OpenNspw.Desktop/Icon.bmp" "$appdir/OpenNSPW.png"

cat > "$appdir/OpenNSPW.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=OpenNSPW
Exec=OpenNspw.Desktop
Icon=OpenNSPW
Categories=Game;StrategyGame;
EOF

# The app looks for Original up from its own directory, which is inside the mounted AppImage: give it the one beside the
# AppImage, unless it is told where the files are.
cat > "$appdir/AppRun" <<'EOF'
#!/bin/sh
here=$(dirname "$(readlink -f "$0")")
original=$(dirname "${APPIMAGE:-$here}")/Original
case " $* " in
*" --data "*) ;;
*) [ -d "$original/NSPW_NET" ] && set -- --data "$original/NSPW_NET" "$@" ;;
esac
case " $* " in
*" --exe "*) ;;
*) [ -f "$original/Nspw122/NSPW_122.exe" ] && set -- --exe "$original/Nspw122/NSPW_122.exe" "$@" ;;
esac
exec "$here/usr/bin/OpenNspw.Desktop" "$@"
EOF
chmod +x "$appdir/AppRun"

download appimagetool https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage \
	ed4ce84f0d9caff66f50bcca6ff6f35aae54ce8135408b3fa33abfc3cb384eb0
download runtime https://github.com/AppImage/type2-runtime/releases/download/20251108/runtime-x86_64 \
	2fca8b443c92510f1483a883f60061ad09b46b978b2631c807cd873a47ec260d
chmod +x "$work/appimagetool"

# Extracted rather than mounted, as FUSE may be missing where it builds.
mkdir -p "$output"
APPIMAGE_EXTRACT_AND_RUN=1 ARCH=x86_64 "$work/appimagetool" --no-appstream --runtime-file "$work/runtime" "$appdir" \
	"$output/OpenNSPW-$version-linux-x64.AppImage"
