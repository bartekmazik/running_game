# Help-ability icons for the UI v2 set. Same style rules as glyphs.py (white silhouette, 128 box,
# live area 14..114, stroke 12 / secondary 8, evenodd holes). Writes Assets/ui/icons/abilities/*.svg
# Usage: python tools/ui-art/abilities_help.py
import math, pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "ui" / "icons" / "abilities"
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


def twinkle(cx, cy, r):
    k = r * 0.22
    return solid(f"M{f(cx)} {f(cy - r)} Q{f(cx + k)} {f(cy - k)} {f(cx + r)} {f(cy)} Q{f(cx + k)} {f(cy + k)} {f(cx)} {f(cy + r)} "
                 f"Q{f(cx - k)} {f(cy + k)} {f(cx - r)} {f(cy)} Q{f(cx - k)} {f(cy - k)} {f(cx)} {f(cy - r)} Z")


def hole(cx, cy, r):
    return f"M{f(cx)} {f(cy - r)} A{r} {r} 0 1 0 {f(cx + 0.1)} {f(cy - r)} Z"


def slit(x1, y1, x2, y2, w=6.5):
    dx, dy = x2 - x1, y2 - y1
    n = math.hypot(dx, dy)
    px, py = -dy / n * w / 2, dx / n * w / 2
    p = [(x1 + px, y1 + py), (x2 + px, y2 + py), (x2 - px, y2 - py), (x1 - px, y1 - py)]
    return "M" + " L".join(f"{f(a)} {f(b)}" for a, b in p) + " Z"


def feather():
    body = "M108 12 Q114 62 76 92 Q48 112 24 104 Q16 66 44 38 Q72 14 108 12 Z"
    # axis from quill base (24,104) to tip (108,12); slits slant toward the tip on both sides
    ax, ay, bx, by = 28, 100, 100, 20
    holes = ""
    for t in (0.3, 0.55):
        cx, cy = ax + (bx - ax) * t, ay + (by - ay) * t
        # perpendicular unit (up-left / down-right) and tipward unit
        for s, L in ((1, 20), (-1, 20)):
            px, py = s * 0.69, s * 0.72  # perpendicular-ish
            tx, ty = 0.72, -0.69
            sx, sy = cx + px * 4, cy + py * 4
            ex, ey = cx + px * L + tx * 7, cy + py * L + ty * 7
            holes += " " + slit(sx, sy, ex, ey)
    return solid(body + holes, True) + line("M30 98 L14 114", 9)


def arch_bridge():
    # deck arc with hangers above a flat road
    out = line("M18 84 Q64 14 110 84", 12)
    for x in (44, 64, 84):
        t = (x - 18) / 92
        y = (1 - t) ** 2 * 84 + 2 * (1 - t) * t * 14 + t * t * 84
        out += line(f"M{x} {f(y)} V96", 8)
    return out + line("M12 100 H116", 12)


ICONS = {
    "superjump": line("M38 50 L64 24 L90 50") + line("M64 26 V68")
                 + line("M36 88 H92", 10) + line("M44 108 H84", 10),
    "doublejump": line("M28 54 L64 22 L100 54") + line("M28 100 L64 68 L100 100"),
    "featherfall": feather(),
    "ghoststep": solid("M28 108 V58 Q28 14 64 14 Q100 14 100 58 V108 Q100 114 94 110 L82 100 Q76 96 70 102 L66 108 Q64 112 62 108 L58 102 Q52 96 46 100 L34 110 Q28 114 28 108 Z "
                       + hole(49, 56, 9) + " " + hole(79, 56, 9), True),
    "ironbody": solid("M64 12 L104 26 Q110 28 110 34 V60 Q110 92 68 116 Q64 118 60 116 Q18 92 18 60 V34 Q18 28 24 26 Z "
                      + hole(64, 62, 21), True) + circle(64, 62, 9),
    "bubbleshield": ring(64, 64, 46, 11) + line("M38 66 A26 26 0 0 1 58 40", 9) + circle(78, 86, 6),
    "patch": "".join(rrect(x, y, 28, 28, 7) for y in (14, 50, 86) for x in (14, 50, 86) if (x, y) != (50, 50))
             + line("M64 54 V74", 10) + line("M54 64 H74", 10),
    "bridge": arch_bridge(),
    "blink": circle(18, 64, 7) + circle(40, 64, 9) + circle(62, 64, 11) + twinkle(96, 64, 30),
    "rewind": line("M28 50 A38 38 0 1 1 24 76", 11) + solid("M44 44 L16 30 L20 62 Z")
              + line("M64 64 V46", 8) + line("M64 64 L78 72", 8),
    "updraft": line("M64 112 Q46 98 64 84 Q80 70 64 54 V30", 11) + line("M42 44 L64 20 L86 44", 11)
               + line("M22 108 Q8 94 22 82 Q32 72 22 62", 9) + line("M106 108 Q92 94 106 82 Q116 72 106 62", 9),
    "giant": rrect(48, 48, 32, 32, 9)
             + "".join(line(d, 10) for d in (
                 "M64 36 V22 M52 32 L64 20 L76 32", "M64 92 V106 M52 96 L64 108 L76 96",
                 "M36 64 H22 M32 52 L20 64 L32 76", "M92 64 H106 M96 52 L108 64 L96 76")),
}

OUT.mkdir(parents=True, exist_ok=True)
for name, body in ICONS.items():
    (OUT / f"{name}.svg").write_text(svg(body), encoding="utf-8")
print("wrote", len(ICONS), "icons to", OUT)
