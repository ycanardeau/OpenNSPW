#!/bin/bash
# Cuts the fonts that come with the app (OpenNSPW/OpenNspw.Desktop/Fonts, BundledFonts) down, in place, to what it can
# draw, a third of their size, for the release builds (release.yml, package-linux-appimage.sh, package-macos-app.sh).
# Runs where fontTools' pyftsubset is (pipx install fonttools).
#
# tools/subset-fonts.sh <the published app's Fonts directory>
#
# The characters of Shift_JIS (code page 932), which is all the game's text can be (ShiftJis), and those of the UI's
# text (Texts.cs and the screens) beyond it: Latin-1 and Latin Extended-A, punctuation, arrows, mathematical operators
# and shapes. Without hinting and layout tables: stb_truetype, which draws all of the app's text (TextRasterizer,
# FontStashSharp), reads neither, and the fonts have no kerning. The SIL Open Font License (OFL.txt) allows this, as
# the fonts have no Reserved Font Name.

set -euo pipefail

fonts=$1
work=$(mktemp -d)

python3 -I - "$work/cp932.txt" <<'EOF'
import sys

characters = set()
for first in range(0x81, 0x100):
	for second in range(0x40, 0x100):
		try:
			characters.update(bytes([first, second]).decode("cp932"))
		except UnicodeDecodeError:
			pass
for single in range(0x20, 0x100):
	try:
		characters.update(bytes([single]).decode("cp932"))
	except UnicodeDecodeError:
		pass
open(sys.argv[1], "w", encoding="utf-8").write("".join(sorted(characters)))
EOF

for font in "$fonts"/*.ttf; do
	pyftsubset "$font" --output-file="$work/subset.ttf" --text-file="$work/cp932.txt" \
		--unicodes="U+0020-007E,U+00A0-017F,U+2000-206F,U+2190-22FF,U+2500-25FF,U+3000-30FF,U+FF00-FFEF" \
		--layout-features= --no-hinting --drop-tables+=GSUB,GPOS,vhea,vmtx,VORG,DSIG,meta
	mv "$work/subset.ttf" "$font"
done

rm -rf "$work"
