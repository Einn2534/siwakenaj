"""Render exact hiragana glyph sprites from the project's Japanese font."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
FONT = ROOT / "Assets/Fonts/NotoSansCJKjp-Regular.otf"
OUTPUT = ROOT / "Assets/Art/Sprites/SortingThemes"
SIZE = 1254
GLYPHS = {"me": "め", "no": "の", "yu": "ゆ"}


def main() -> None:
    font = ImageFont.truetype(str(FONT), 940)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, glyph in GLYPHS.items():
        image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        left, top, right, bottom = draw.textbbox((0, 0), glyph, font=font, stroke_width=25)
        position = ((SIZE - left - right) / 2, (SIZE - top - bottom) / 2)
        draw.text(position, glyph, font=font, fill=(255, 212, 126, 255),
                  stroke_width=25, stroke_fill=(20, 34, 60, 255))
        image.save(OUTPUT / f"hiragana-{name}.png")


if __name__ == "__main__":
    main()
