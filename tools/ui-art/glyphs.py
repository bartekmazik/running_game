# UI v2 glyph set: single-colour white silhouettes, no outline, no shadow. The UI tints them with
# background-image-tint, so one file serves every context (cream on a sage tile, ink on paper...).
#
# Construction rules, so every glyph has the same weight:
#   * 128 box, live area 14..114.
#   * Line glyphs: stroke 12, round caps and joins. Secondary lines: 8.
#   * Solid glyphs: rounded corners (r >= 6); holes are cut with fill-rule evenodd.
#   * One idea per glyph, readable at 24 px.
#
# Usage: python tools/ui-art/glyphs.py   (writes Assets/ui/icons/<set>/<name>.svg)
import math, pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "ui" / "icons"
W = "#FFFFFF"


def svg(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 128 128">\n{body}\n</svg>\n'


def line(d, w=12):
    return f'<path d="{d}" fill="none" stroke="{W}" stroke-width="{w}" stroke-linecap="round" stroke-linejoin="round"/>'


def solid(d, evenodd=False):
    rule = ' fill-rule="evenodd"' if evenodd else ""
    return f'<path d="{d}" fill="{W}"{rule}/>'


def rrect(x, y, w, h, r):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{W}"/>'


def circle(cx, cy, r):
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{W}"/>'


def ring(cx, cy, r, w=12):
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="none" stroke="{W}" stroke-width="{w}"/>'


def f(v):
    return f"{v:.1f}".rstrip("0").rstrip(".")


def star(cx, cy, r, ri, n=5, rot=-90):
    pts = []
    for i in range(n * 2):
        rr = r if i % 2 == 0 else ri
        a = math.radians(rot + i * 180 / n)
        pts.append(f"{f(cx + rr * math.cos(a))},{f(cy + rr * math.sin(a))}")
    return f'<polygon points="{" ".join(pts)}" fill="{W}" stroke="{W}" stroke-width="6" stroke-linejoin="round"/>'


def twinkle(cx, cy, r):
    # Four-point sparkle with concave sides.
    k = r * 0.22
    return solid(f"M{f(cx)} {f(cy - r)} Q{f(cx + k)} {f(cy - k)} {f(cx + r)} {f(cy)} Q{f(cx + k)} {f(cy + k)} {f(cx)} {f(cy + r)} "
                 f"Q{f(cx - k)} {f(cy + k)} {f(cx - r)} {f(cy)} Q{f(cx - k)} {f(cy - k)} {f(cx)} {f(cy - r)} Z")


def snowflake():
    out = []
    for k in range(3):
        a = math.radians(90 + 60 * k)
        dx, dy = math.cos(a), math.sin(a)
        out.append(line(f"M{f(64 - 48 * dx)} {f(64 - 48 * dy)} L{f(64 + 48 * dx)} {f(64 + 48 * dy)}", 10))
    for k in range(6):
        a = math.radians(-90 + 60 * k)
        dx, dy = math.cos(a), math.sin(a)
        px, py = -dy, dx
        bx, by = 64 + 24 * dx, 64 + 24 * dy
        for side in (1, -1):
            tx = 64 + 38 * dx + side * 14 * px
            ty = 64 + 38 * dy + side * 14 * py
            out.append(line(f"M{f(bx)} {f(by)} L{f(tx)} {f(ty)}", 8))
    return "".join(out)


ABILITIES = {
    "dash": line("M60 30 L94 64 L60 98") + line("M16 44 H40", 9) + line("M10 64 H44", 9) + line("M16 84 H40", 9),
    "shove": (rrect(32, 58, 60, 50, 20)
              + rrect(34, 30, 13, 44, 6.5) + rrect(50, 22, 13, 50, 6.5) + rrect(66, 26, 13, 46, 6.5) + rrect(82, 36, 12, 38, 6)
              + line("M40 84 L22 64", 13)
              + line("M106 40 L114 34", 7) + line("M108 64 H118", 7) + line("M106 88 L114 94", 7)),
    "turbo": solid("M72 12 L28 72 H58 L48 116 L100 50 H68 Z"),
    "tntbomb": circle(56, 76, 36) + rrect(62, 30, 22, 18, 5) + line("M76 32 Q82 18 96 18", 6) + twinkle(102, 16, 13),
    "ice": snowflake(),
    "hook": ring(64, 20, 8, 7) + line("M64 30 V78 A20 20 0 1 1 24 78") + solid("M24 64 L12 84 L36 84 Z"),
}

GLYPHS = {
    "back": line("M54 30 L20 64 L54 98") + line("M22 64 H106"),
    "close": line("M32 32 L96 96") + line("M96 32 L32 96"),
    "play": solid("M44 26 Q36 22 36 32 V96 Q36 106 44 102 L100 70 Q108 64 100 58 Z"),
    "star": star(64, 66, 46, 21),
    "players": circle(48, 40, 18) + solid("M16 106 Q16 70 48 70 Q80 70 80 106 Z")
               + circle(88, 46, 14) + solid("M84 104 Q86 80 76 72 Q82 64 92 64 Q114 64 114 96 V104 Z"),
    "clock": ring(64, 70, 40, 11) + line("M64 70 V48", 10) + line("M64 70 L80 80", 10) + line("M54 16 H74", 10),
    "hourglass": rrect(30, 14, 68, 12, 5) + rrect(30, 102, 68, 12, 5)
                 + line("M40 26 Q40 50 64 64 Q88 50 88 26 M40 102 Q40 78 64 64 Q88 78 88 102", 9)
                 + solid("M46 100 Q50 84 64 80 Q78 84 82 100 Z"),
    "trophy": solid("M36 18 H92 V46 Q92 76 64 80 Q36 76 36 46 Z")
              + line("M36 30 H26 Q16 30 18 44 Q22 58 38 60", 9) + line("M92 30 H102 Q112 30 110 44 Q106 58 90 60", 9)
              + rrect(57, 78, 14, 20, 3) + rrect(38, 96, 52, 14, 6),
    "flame": solid("M64 10 Q86 38 90 62 Q94 92 74 106 Q64 114 50 108 Q32 100 34 76 Q36 60 50 50 Q50 66 60 70 Q56 46 64 10 Z"),
    "runner": circle(76, 22, 11)
              + line("M70 38 L60 68", 12)
              + line("M68 44 L84 56 L96 48", 10) + line("M66 46 L50 54 L40 44", 10)
              + line("M60 68 L78 84 L76 108", 11) + line("M60 68 L46 88 L26 92", 11),
    "eye": solid("M8 64 Q64 8 120 64 Q64 120 8 64 Z M64 40 A24 24 0 1 0 64.1 40 Z", True) + circle(64, 64, 13),
    "impact": star(64, 64, 52, 26, 8, -90),
    "lock": line("M44 56 V40 Q44 20 64 20 Q84 20 84 40 V56", 11)
            + solid("M38 54 H90 Q100 54 100 64 V102 Q100 112 90 112 H38 Q28 112 28 102 V64 Q28 54 38 54 Z "
                    "M64 72 A9 9 0 1 0 64.1 72 Z", True),
    "layers": solid("M64 16 L112 40 L64 64 L16 40 Z") + line("M18 62 L64 86 L110 62", 10) + line("M18 84 L64 108 L110 84", 10),
    "bolt": solid("M72 12 L28 72 H58 L48 116 L100 50 H68 Z"),
    "heart": solid("M64 108 Q16 76 16 46 Q16 22 40 20 Q56 20 64 36 Q72 20 88 20 Q112 22 112 46 Q112 76 64 108 Z"),
    "dice": solid("M34 18 H94 Q110 18 110 34 V94 Q110 110 94 110 H34 Q18 110 18 94 V34 Q18 18 34 18 Z "
                  "M44 34 A10 10 0 1 0 44.1 34 Z M84 34 A10 10 0 1 0 84.1 34 Z M64 54 A10 10 0 1 0 64.1 54 Z "
                  "M44 74 A10 10 0 1 0 44.1 74 Z M84 74 A10 10 0 1 0 84.1 74 Z", True),
    "door": solid("M28 18 Q28 12 34 12 H72 Q78 12 78 18 V110 Q78 116 72 116 H34 Q28 116 28 110 Z M62 58 A6 6 0 1 0 62.1 58 Z", True)
            + line("M90 64 H116 M104 52 L116 64 L104 76", 10),
    "refresh": line("M100 50 A38 38 0 1 0 104 76", 11) + solid("M84 44 L112 30 L108 62 Z"),
    "search": ring(54, 54, 30, 12) + line("M76 76 L108 108", 14),
    "host": solid("M64 14 L116 56 Q118 62 112 62 H104 V106 Q104 114 96 114 H32 Q24 114 24 106 V62 H16 Q10 62 12 56 Z "
                  "M54 80 Q54 74 60 74 H68 Q74 74 74 80 V114 H54 Z", True),
    "medal": solid("M38 10 H58 L70 50 L54 56 Z") + solid("M90 10 H70 L58 50 L74 56 Z")
             + solid("M64 50 A34 34 0 1 0 64.1 50 Z M64 66 A18 18 0 1 0 64.1 66 Z", True),
    "check": line("M24 66 L52 94 L106 34", 14),
    "crown": solid("M18 40 L42 62 L64 24 L86 62 L110 40 L100 96 H28 Z") + rrect(28, 100, 72, 12, 5),
    "question": line("M40 44 Q40 18 64 18 Q88 18 88 42 Q88 58 72 64 Q64 68 64 80", 13) + circle(64, 104, 9),
    "sparkle": twinkle(64, 64, 50),
}


def write(group, items):
    folder = OUT / group
    folder.mkdir(parents=True, exist_ok=True)
    for name, body in items.items():
        (folder / f"{name}.svg").write_text(svg(body), encoding="utf-8")
    return len(items)


n = write("abilities", ABILITIES) + write("glyphs", GLYPHS)
print("wrote", n, "glyphs to", OUT)
