#!/bin/bash
# Builds OpenNSPW.app in the output directory: the desktop app for Apple Silicon Macs, self-contained in a single-file
# executable, ad-hoc signed, as release.yml publishes it. Runs on macOS only, for sips, iconutil and codesign.
#
# tools/package-macos-app.sh <version> <output directory>
#
# The app finds the original's files (Original/) beside it, as it finds them beside the Windows build.

set -euo pipefail

version=$1
app=$2/OpenNSPW.app
root=$(dirname "$0")/..

rm -rf "$app"

# One executable, with only what it reads from its own directory beside it: libSDL2-2.0.0.dylib and libopenal.dylib,
# which MonoGame loads from there, and Fonts.
dotnet publish "$root/OpenNSPW/OpenNspw.Desktop" --configuration Release --runtime osx-arm64 --self-contained \
	/p:PublishSingleFile=true /p:DebugType=embedded /p:GenerateDocumentationFile=false /p:Version="$version" \
	--output "$app/Contents/MacOS"

# The icon from the window's (Icon.bmp, the original's 32x32 icon), at the sizes it has; Finder scales it up.
iconset=$(mktemp -d)/OpenNSPW.iconset
mkdir "$iconset"
sips -s format png "$root/OpenNSPW/OpenNspw.Desktop/Icon.bmp" --out "$iconset/icon_32x32.png" > /dev/null
cp "$iconset/icon_32x32.png" "$iconset/icon_16x16@2x.png"
sips -z 16 16 "$iconset/icon_32x32.png" --out "$iconset/icon_16x16.png" > /dev/null
mkdir "$app/Contents/Resources"
iconutil --convert icns "$iconset" --output "$app/Contents/Resources/OpenNSPW.icns"

# Fonts in Resources, with a link where the app reads them: codesign keeps the signatures of files other than code in
# MacOS in extended attributes, which most unzipping loses.
mv "$app/Contents/MacOS/Fonts" "$app/Contents/Resources/Fonts"
ln -s ../Resources/Fonts "$app/Contents/MacOS/Fonts"

# The bundle's versions are numbers only, so a prerelease such as 0.0.1-alpha.4 is 0.0.1.
cat > "$app/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleDevelopmentRegion</key>
	<string>en</string>
	<key>CFBundleDisplayName</key>
	<string>OpenNSPW</string>
	<key>CFBundleExecutable</key>
	<string>OpenNspw.Desktop</string>
	<key>CFBundleIconFile</key>
	<string>OpenNSPW</string>
	<key>CFBundleIdentifier</key>
	<string>io.github.ycanardeau.OpenNSPW</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundleName</key>
	<string>OpenNSPW</string>
	<key>CFBundlePackageType</key>
	<string>APPL</string>
	<key>CFBundleShortVersionString</key>
	<string>${version%%-*}</string>
	<key>CFBundleVersion</key>
	<string>${version%%-*}</string>
	<key>LSApplicationCategoryType</key>
	<string>public.app-category.strategy-games</string>
	<key>NSHighResolutionCapable</key>
	<true/>
</dict>
</plist>
EOF

# Apple Silicon runs only signed code: sign the libraries and the bundle, with no identity (ad-hoc).
codesign --force --deep --sign - "$app"
