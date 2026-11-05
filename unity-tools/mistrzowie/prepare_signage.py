"""Five authored metro sign atlases using the existing owner's city-metro artwork.

Source image, logo, pictograms and emergency symbols are preserved.
Route names, arrivals, map and both platform destination panels are corrected.
No departure service is advertised before a verified moving-train runtime.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from layout import METRO, METRO_ROUTE_ORDER

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "unity-city/Assets/Source/Claude/Wnetrza/metro_atlas.png"
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie"
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"

DESTINATIONS = {
    "M1": ("DWORZEC / PKP", "GALERIA MIEJSKA"),
    "M2": ("KASYNA / BIURA", "CENTRUM MIASTA"),
    "M3": ("ARENA A1 / HOTELE", "MISTRZOSTWA"),
    "M4": ("ARENA A2 / BOKS", "KICKBOXING / LIGA"),
    "M5": ("GALERIA GŁÓWNA", "KLUBY / USŁUGI"),
}


def panel(draw, rect, text, maxsize=48):
    """Preserve old atlas UV rectangles and fit Polish text within each one."""
    x, y, w, h = rect
    draw.rectangle((x, y, x+w-1, y+h-1), fill="#101d2b")
    font = None
    for size in range(maxsize, 7, -1):
        font = ImageFont.truetype(FONT, size)
        bounds = draw.multiline_textbbox((0, 0), text, font=font, spacing=5)
        if bounds[2]-bounds[0] <= w-22 and bounds[3]-bounds[1] <= h-12:
            break
    if bounds[2]-bounds[0] > w-22 or bounds[3]-bounds[1] > h-12:
        raise ValueError("Metro panel too small for text: "+text)
    draw.multiline_text((x+11, y+(h-(bounds[3]-bounds[1]))/2-bounds[1]),
                        text, font=font, fill="#f4eee3", spacing=5)


def render_all(source=SOURCE, destination=DEST):
    if not source.is_file():
        raise FileNotFoundError("Original city metro atlas required: "+str(source))
    if set(DESTINATIONS) != {s["id"] for s in METRO} or len(METRO) != 5:
        raise ValueError("Station names and destination boards must match 5-station layout")
    with Image.open(source) as original:
        base = original.convert("RGB")
    if base.size != (1024, 1024):
        raise ValueError("Original city metro UV atlas must remain 1024x1024")
    destination.mkdir(parents=True, exist_ok=True)
    display = {s["id"]: s["name"] for s in METRO}
    summary = "MIASTO MISTRZÓW\n" + "\n".join(
        s["id"]+" "+s["name"] for s in METRO)
    route = "    /    ".join(
        sid+" "+display[sid] for sid in METRO_ROUTE_ORDER)
    results = []
    for station in METRO:
        sid, name = station["id"], station["name"]
        img = base.copy()
        draw = ImageDraw.Draw(img)
        panel(draw, (256, 0, 768, 128), sid+"  "+name, 65)        # aNazwa
        panel(draw, (256, 128, 768, 64), "BRAXHORN METRO  /  MIASTO MISTRZÓW", 36)
        panel(draw, (256, 192, 512, 64), "PERON 1   /   PERON 2", 34)
        panel(draw, (768, 192, 256, 64), "INFORMACJA", 29)
        panel(draw, (0, 256, 512, 64), "WYJŚCIE  /  MIASTO", 32)
        panel(draw, (512, 256, 512, 64), "WYJŚCIE  /  MIASTO", 32)
        panel(draw, (0, 320, 512, 192), "METRO W PRZYGOTOWANIU\nPOCIĄGI NIE KURSUJĄ", 34)
        panel(draw, (512, 320, 512, 192), summary, 26)
        panel(draw, (640, 576, 384, 64), "KASA / INFORMACJA", 31)
        panel(draw, (512, 640, 512, 64), "METRO MIASTO MISTRZÓW", 29)
        panel(draw, (0, 640, 256, 64), "STACJA "+sid, 33)
        panel(draw, (768, 704, 128, 64), "NIE\nKURSUJE", 25)
        panel(draw, (0, 832, 1024, 64), route, 26)
        panel(draw, (0, 896, 512, 128), DESTINATIONS[sid][0], 43)
        panel(draw, (512, 896, 512, 128), DESTINATIONS[sid][1], 43)
        output = destination / ("metro_"+sid+".png")
        img.save(output, format="PNG")
        results.append(output)
    print("BRAX_MISTRZOWIE_SIGNAGE_OK original_city_atlas=unchanged "
          "station_variants=5 no_false_departures=true")
    return results


if __name__ == "__main__":
    render_all()
