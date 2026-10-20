# UI v2 rank emblems: one pennant silhouette for every tier, like the "LEVEL 2" badge in the user's
# reference (Assets/gui reference/game-ui-examples-snufkin.png). The tier is told by colour and by
# the motif on the pennant; the division by 1-3 chevrons in its point. Flat fills, a soft top-light
# gradient and a thin inner keyline. No outlines, no wings, no glow.
#
# Usage: python tools/ui-art/ranks.py   (writes Assets/ui/ranks/)
import math, pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "ui" / "ranks"
OUT.mkdir(parents=True, exist_ok=True)

CREAM = "#FFF8EC"

# body, light (top of the gradient and keyline), deep (shade)
TIERS = {
    "bronze":   ("#A86D4A", "#C98E68", "#8A5536"),
    "silver":   ("#8796A6", "#B0BCC8", "#6C7A8A"),
    "gold":     ("#C99A3E", "#E5BE6A", "#A87C2A"),
    "platinum": ("#5C9A92", "#86C0B8", "#467C75"),
    "diamond":  ("#6D7DBE", "#98A6DB", "#55639F"),
    "master":   ("#6B4A7B", "#94739F", "#533862"),
}
GOLD_TRIM = "#E5BE6A"


def f(v):
    return f"{v:.1f}".rstrip("0").rstrip(".")


def pts(p):
    return " ".join(f"{f(x)},{f(y)}" for x, y in p)


def pennant(inset=0.0):
    # Flat top with rounded corners, straight sides, a point at the bottom.
    x0, x1, y0, ys, yp = 52 + inset, 204 - inset, 18 + inset, 164, 234 - inset * 1.6
    r = 14 - inset * 0.5
    return (f"M{f(x0 + r)} {f(y0)} H{f(x1 - r)} Q{f(x1)} {f(y0)} {f(x1)} {f(y0 + r)} V{f(ys)} "
            f"Q{f(x1)} {f(ys + 6)} {f(x1 - 5)} {f(ys + 10)} L{f(132)} {f(yp - 2)} Q128 {f(yp + 1)} {f(124)} {f(yp - 2)} "
            f"L{f(x0 + 5)} {f(ys + 10)} Q{f(x0)} {f(ys + 6)} {f(x0)} {f(ys)} V{f(y0 + r)} Q{f(x0)} {f(y0)} {f(x0 + r)} {f(y0)} Z")


def chevrons(n, colour):
    out = []
    for i in range(n):
        y = 168 + i * 18
        out.append(f'<path d="M104 {y} L128 {y + 14} L152 {y}" fill="none" stroke="{colour}" stroke-width="9" '
                   f'stroke-linecap="round" stroke-linejoin="round"/>')
    return "".join(out)


# --- Motifs, all cream, centred on (128, 92) --------------------------------------------------------

def cube(cx, cy, s, opacity=1.0):
    top = [(cx, cy - 26 * s), (cx + 30 * s, cy - 11 * s), (cx, cy + 4 * s), (cx - 30 * s, cy - 11 * s)]
    left = [(cx - 30 * s, cy - 11 * s), (cx, cy + 4 * s), (cx, cy + 34 * s), (cx - 30 * s, cy + 19 * s)]
    right = [(cx + 30 * s, cy - 11 * s), (cx, cy + 4 * s), (cx, cy + 34 * s), (cx + 30 * s, cy + 19 * s)]
    return (f'<g opacity="{opacity}"><polygon points="{pts(top)}" fill="{CREAM}"/>'
            f'<polygon points="{pts(left)}" fill="{CREAM}" fill-opacity="0.78"/>'
            f'<polygon points="{pts(right)}" fill="{CREAM}" fill-opacity="0.56"/></g>')


def star(cx, cy, r, ri=None, n=5):
    ri = ri or r * 0.46
    p = []
    for i in range(n * 2):
        rr = r if i % 2 == 0 else ri
        a = math.radians(-90 + i * 180 / n)
        p.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return f'<polygon points="{pts(p)}" fill="{CREAM}" stroke="{CREAM}" stroke-width="8" stroke-linejoin="round"/>'


def hex_gem(cx, cy, r):
    outer = [(cx + r * math.cos(math.radians(-90 + 60 * i)), cy + r * math.sin(math.radians(-90 + 60 * i))) for i in range(6)]
    inner = [(cx + r * 0.52 * math.cos(math.radians(-90 + 60 * i)), cy + r * 0.52 * math.sin(math.radians(-90 + 60 * i))) for i in range(6)]
    faces = []
    for i in range(6):
        quad = [outer[i], outer[(i + 1) % 6], inner[(i + 1) % 6], inner[i]]
        faces.append(f'<polygon points="{pts(quad)}" fill="{CREAM}" fill-opacity="{[1, .82, .62, .55, .72, .9][i]}"/>')
    return "".join(faces) + f'<polygon points="{pts(inner)}" fill="{CREAM}"/>'


def brilliant(cx, cy, s):
    top, gird, tip = cy - 26 * s, cy - 8 * s, cy + 34 * s
    w, tw = 40 * s, 22 * s
    crown_l = [(cx - tw, top), (cx, top), (cx - 12 * s, gird), (cx - w, gird)]
    crown_r = [(cx, top), (cx + tw, top), (cx + w, gird), (cx + 12 * s, gird)]
    crown_c = [(cx, top), (cx + 12 * s, gird), (cx - 12 * s, gird)]
    pav_l = [(cx - w, gird), (cx - 12 * s, gird), (cx, tip)]
    pav_c = [(cx - 12 * s, gird), (cx + 12 * s, gird), (cx, tip)]
    pav_r = [(cx + 12 * s, gird), (cx + w, gird), (cx, tip)]
    parts = [(crown_l, .9), (crown_c, 1), (crown_r, .7), (pav_l, .78), (pav_c, .6), (pav_r, .48)]
    return "".join(f'<polygon points="{pts(p)}" fill="{CREAM}" fill-opacity="{o}"/>' for p, o in parts)


def crown(cx, cy, s, colour=CREAM):
    w, h = 44 * s, 34 * s
    p = [(cx - w, cy + h * 0.5), (cx - w - 4 * s, cy - h * 0.55), (cx - w * 0.45, cy - h * 0.05), (cx, cy - h * 0.8),
         (cx + w * 0.45, cy - h * 0.05), (cx + w + 4 * s, cy - h * 0.55), (cx + w, cy + h * 0.5)]
    band = f'<rect x="{f(cx - w)}" y="{f(cy + h * 0.5)}" width="{f(2 * w)}" height="{f(12 * s)}" rx="{f(4 * s)}" fill="{colour}"/>'
    gems = "".join(f'<circle cx="{f(x)}" cy="{f(y)}" r="{f(6 * s)}" fill="{colour}"/>'
                   for x, y in [(cx - w - 4 * s, cy - h * 0.55), (cx, cy - h * 0.8), (cx + w + 4 * s, cy - h * 0.55)])
    return f'<polygon points="{pts(p)}" fill="{colour}" stroke="{colour}" stroke-width="6" stroke-linejoin="round"/>' + band + gems


MOTIF = {
    "bronze": lambda: cube(128, 88, 1.25),
    "silver": lambda: cube(128, 104, 0.85) + cube(128, 70, 0.85, 0.85),
    "gold": lambda: star(128, 96, 44),
    "platinum": lambda: hex_gem(128, 94, 44),
    "diamond": lambda: brilliant(128, 92, 1.25),
    "master": lambda: crown(128, 96, 1.0),
}


def emblem(tier, division):
    body, light, deep = TIERS[tier]
    gid = f"{tier}{division}"
    trim = GOLD_TRIM if tier == "master" else light
    parts = [
        f'<defs><linearGradient id="{gid}g" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{light}"/><stop offset="0.55" stop-color="{body}"/><stop offset="1" stop-color="{deep}"/>'
        f'</linearGradient></defs>',
        # Soft contact shadow so the badge sits on the card, not floating.
        f'<path d="{pennant()}" transform="translate(0 6)" fill="#2E2A26" fill-opacity="0.16"/>',
        f'<path d="{pennant()}" fill="url(#{gid}g)"/>',
        f'<path d="{pennant(9)}" fill="none" stroke="{trim}" stroke-width="3" stroke-opacity="0.75"/>',
        MOTIF[tier](),
        chevrons(3 if tier == "master" else division, GOLD_TRIM if tier == "master" else CREAM),
    ]
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 256 256">\n' + "\n".join(parts) + "\n</svg>\n"


# --- Podium medals: a plain disc on a short ribbon -------------------------------------------------

DIGITS = {
    1: "M-8 -20 L4 -28 V28",
    2: "M-16 -16 Q-14 -30 0 -30 Q16 -30 16 -16 Q16 -6 -16 28 H18",
    3: "M-14 -24 Q-4 -32 6 -30 Q18 -26 16 -14 Q14 -4 0 -2 Q18 0 18 12 Q18 30 0 30 Q-10 30 -18 22",
}


def medal(place):
    body, light, deep = TIERS[{1: "gold", 2: "silver", 3: "bronze"}[place]]
    gid = f"m{place}"
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 256 256">
<defs><linearGradient id="{gid}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{light}"/><stop offset="1" stop-color="{deep}"/></linearGradient></defs>
<polygon points="84,10 120,10 140,96 108,104" fill="#7EA3C0"/>
<polygon points="172,10 136,10 116,96 148,104" fill="#D9845A"/>
<circle cx="128" cy="160" r="78" fill="#2E2A26" fill-opacity="0.16" transform="translate(0 6)"/>
<circle cx="128" cy="160" r="78" fill="url(#{gid})"/>
<circle cx="128" cy="160" r="62" fill="none" stroke="{light}" stroke-width="4" stroke-opacity="0.8"/>
<path d="{DIGITS[place]}" transform="translate(128 160) scale(1.4)" fill="none" stroke="{CREAM}" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
'''


for tier in ("bronze", "silver", "gold", "platinum", "diamond"):
    for division in (1, 2, 3):
        (OUT / f"{tier}_{division}.svg").write_text(emblem(tier, division), encoding="utf-8")
(OUT / "master.svg").write_text(emblem("master", 1), encoding="utf-8")
for place in (1, 2, 3):
    (OUT / f"medal_{place}.svg").write_text(medal(place), encoding="utf-8")
print("wrote", len(list(OUT.glob("*.svg"))), "files to", OUT)
