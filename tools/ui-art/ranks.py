# Rank emblem generator — writes Assets/ui/ranks/<tier>_<division>.svg (and master.svg) plus the
# podium medals Assets/ui/ranks/medal_<place>.svg. Every emblem is composed from the same parts so
# the set reads as one family: back ornament (wings / laurels / rays), tier frame with a metal
# gradient, an inset face with the tier symbol, and a ribbon carrying the division pips.
#
# Usage: python ranks.py   (from anywhere; paths are resolved against the project root)
import math, os, pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "ui" / "ranks"
OUT.mkdir(parents=True, exist_ok=True)

INK = "#2B2142"
LINE = "#3D2E5C"
OUTLINE = 8

# hi, mid, dark, face-top, face-bottom, accent (symbol)
TIERS = {
    "bronze":   ("#FBD0A6", "#DE8F5A", "#93502B", "#7A3F22", "#4E2616", "#FFC79A"),
    "silver":   ("#FFFFFF", "#C9D6E8", "#7E90AE", "#5B6C8C", "#36405A", "#EAF2FF"),
    "gold":     ("#FFF3B0", "#FFC542", "#C7841A", "#8A5414", "#55320C", "#FFE27A"),
    "platinum": ("#E4FFFB", "#74E3D8", "#2B9C96", "#1F6E70", "#123F44", "#B6FFF6"),
    "diamond":  ("#F2EDFF", "#AC95FF", "#6447D6", "#45309C", "#261A5E", "#DCD2FF"),
    "master":   ("#FFF4B8", "#FFD24D", "#CF861A", "#6A2E8C", "#3A1554", "#FF8FD2"),
}

CX, CY = 128.0, 118.0


def f(v):
    return f"{v:.1f}".rstrip("0").rstrip(".")


def pts(points):
    return " ".join(f"{f(x)},{f(y)}" for x, y in points)


def defs(t, p):
    hi, mid, dark, ft, fb, acc = p
    return f"""<defs>
<linearGradient id="{t}-metal" x1="0" y1="0" x2="0" y2="1">
<stop offset="0" stop-color="{hi}"/><stop offset="0.45" stop-color="{mid}"/><stop offset="1" stop-color="{dark}"/></linearGradient>
<linearGradient id="{t}-face" x1="0" y1="0" x2="0" y2="1">
<stop offset="0" stop-color="{ft}"/><stop offset="1" stop-color="{fb}"/></linearGradient>
<linearGradient id="{t}-sym" x1="0" y1="0" x2="1" y2="1">
<stop offset="0" stop-color="#FFFFFF"/><stop offset="0.35" stop-color="{acc}"/><stop offset="1" stop-color="{mid}"/></linearGradient>
<radialGradient id="{t}-glow" cx="0.5" cy="0.5" r="0.5">
<stop offset="0" stop-color="{hi}" stop-opacity="0.9"/><stop offset="0.6" stop-color="{mid}" stop-opacity="0.35"/><stop offset="1" stop-color="{mid}" stop-opacity="0"/></radialGradient>
</defs>"""


# --- Frames: each returns (outer path d, inner face path d) ---------------------------------------

def scalloped_circle(cx, cy, r, bumps, depth):
    d = []
    n = bumps * 2
    for i in range(n + 1):
        a = -math.pi / 2 + i * 2 * math.pi / n
        rr = r if i % 2 == 0 else r - depth
        x, y = cx + rr * math.cos(a), cy + rr * math.sin(a)
        if i == 0:
            d.append(f"M{f(x)} {f(y)}")
        else:
            am = -math.pi / 2 + (i - 0.5) * 2 * math.pi / n
            qr = r + depth * 0.6 if i % 2 == 0 else r + depth * 0.6
            qx, qy = cx + qr * math.cos(am), cy + qr * math.sin(am)
            d.append(f"Q{f(qx)} {f(qy)} {f(x)} {f(y)}")
    return " ".join(d) + " Z"


def circle_d(cx, cy, r):
    return f"M{f(cx - r)} {f(cy)} A{f(r)} {f(r)} 0 1 0 {f(cx + r)} {f(cy)} A{f(r)} {f(r)} 0 1 0 {f(cx - r)} {f(cy)} Z"


def shield_d(s=1.0, crown=False):
    # Heater shield centred on CX, top at ~CY-84, point at ~CY+92, scaled by s about (CX, CY).
    def P(x, y):
        return f"{f(CX + (x - 128) * s)} {f(CY + (y - 118) * s)}"
    if crown:
        return (f"M{P(54, 52)} L{P(76, 34)} L{P(96, 50)} L{P(128, 24)} L{P(160, 50)} L{P(180, 34)} L{P(202, 52)} "
                f"L{P(202, 112)} C{P(202, 160)} {P(172, 194)} {P(128, 212)} C{P(84, 194)} {P(54, 160)} {P(54, 112)} Z")
    return (f"M{P(128, 30)} C{P(158, 42)} {P(184, 44)} {P(202, 40)} L{P(202, 112)} "
            f"C{P(202, 160)} {P(172, 194)} {P(128, 212)} C{P(84, 194)} {P(54, 160)} {P(54, 112)} L{P(54, 40)} "
            f"C{P(72, 44)} {P(98, 42)} {P(128, 30)} Z")


def poly_d(cx, cy, r, n, rot):
    p = [(cx + r * math.cos(rot + i * 2 * math.pi / n), cy + r * math.sin(rot + i * 2 * math.pi / n)) for i in range(n)]
    return "M" + " L".join(f"{f(x)} {f(y)}" for x, y in p) + " Z"


def frame(tier):
    if tier == "bronze":
        return scalloped_circle(CX, CY, 84, 14, 7), circle_d(CX, CY, 62)
    if tier == "silver":
        return shield_d(1.0), shield_d(0.74)
    if tier == "gold":
        return shield_d(1.0, crown=True), shield_d(0.72)
    if tier == "platinum":
        return poly_d(CX, CY, 92, 6, -math.pi / 2), poly_d(CX, CY, 68, 6, -math.pi / 2)
    if tier == "diamond":
        outer = f"M{f(CX)} {f(CY - 96)} L{f(CX + 92)} {f(CY)} L{f(CX)} {f(CY + 96)} L{f(CX - 92)} {f(CY)} Z"
        inner = f"M{f(CX)} {f(CY - 68)} L{f(CX + 65)} {f(CY)} L{f(CX)} {f(CY + 68)} L{f(CX - 65)} {f(CY)} Z"
        return outer, inner
    # master: wide crowned shield
    return shield_d(1.04, crown=True), shield_d(0.74)


# --- Back ornaments ---------------------------------------------------------------------------

def feather(ax, ay, angle, length, width, fill):
    # Feather pointing left from its root (ax, ay), rotated by angle degrees.
    L, W = length, width
    d = f"M0 0 C{f(-L * 0.25)} {f(-W)} {f(-L * 0.75)} {f(-W * 1.1)} {f(-L)} {f(-W * 0.2)} C{f(-L * 0.7)} {f(W * 0.7)} {f(-L * 0.3)} {f(W * 0.8)} 0 0 Z"
    return (f'<path d="{d}" transform="translate({f(ax)} {f(ay)}) rotate({f(angle)})" fill="{fill}" '
            f'stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>')


def wings(t, count, span, root_x, ay):
    # A fan of feathers rising from behind the frame edge: the top feather is the longest.
    left = []
    for i in reversed(range(count)):
        k = i / max(1, count - 1)
        angle = 30 - 50 * k
        length = span * (1.0 - 0.28 * k)
        left.append(feather(root_x, ay + 16 * k, angle, length, 19 - 5 * k, f"url(#{t}-metal)"))
    g = "".join(left)
    return f'<g>{g}</g><g transform="translate(256 0) scale(-1 1)">{g}</g>'


def laurels(t):
    # A branch curving up each side from the ribbon: pointed leaves alternating either side of a stem.
    r, cy = 100, CY + 6
    a0, a1 = 96, 226
    stem = []
    leaves = []
    for i in range(10):
        a = math.radians(a0 + (a1 - a0) * i / 9)
        x, y = CX + r * math.cos(a), cy + r * math.sin(a)
        stem.append((x, y))
        if i == 0:
            continue
        side = 1 if i % 2 else -1
        rot = math.degrees(a) + 90 + side * 32
        size = 1.0 - 0.3 * i / 9
        leaves.append(f'<path d="M0 0 Q{f(12 * size)} {f(-13 * size)} {f(36 * size)} 0 Q{f(12 * size)} {f(13 * size)} 0 0 Z" transform="translate({f(x)} {f(y)}) rotate({f(rot + 180)})" '
                      f'fill="url(#{t}-metal)" stroke="{INK}" stroke-width="4.5" stroke-linejoin="round"/>')
    stem_d = "M" + " L".join(f"{f(x)} {f(y)}" for x, y in stem)
    g = f'<path d="{stem_d}" fill="none" stroke="{TIERS[t][2]}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>' + "".join(leaves)
    return f'<g>{g}</g><g transform="translate(256 0) scale(-1 1)">{g}</g>'


def rays(t, n=12, r0=40, r1=124):
    out = []
    for i in range(n):
        a = i * 2 * math.pi / n
        w = math.pi / n * 0.55
        p = [(CX + r0 * math.cos(a - w), CY + r0 * math.sin(a - w)), (CX + r1 * math.cos(a), CY + r1 * math.sin(a)),
             (CX + r0 * math.cos(a + w), CY + r0 * math.sin(a + w))]
        out.append(f'<polygon points="{pts(p)}" fill="{TIERS[t][1]}" fill-opacity="0.55"/>')
    return f'<circle cx="{f(CX)}" cy="{f(CY)}" r="120" fill="url(#{t}-glow)"/>' + "".join(out)


# --- Symbols on the face ----------------------------------------------------------------------

def tile_cube(t, cx, cy, s):
    # The TNT Run tile as an isometric block.
    top = [(cx, cy - 26 * s), (cx + 30 * s, cy - 11 * s), (cx, cy + 4 * s), (cx - 30 * s, cy - 11 * s)]
    left = [(cx - 30 * s, cy - 11 * s), (cx, cy + 4 * s), (cx, cy + 30 * s), (cx - 30 * s, cy + 15 * s)]
    right = [(cx + 30 * s, cy - 11 * s), (cx, cy + 4 * s), (cx, cy + 30 * s), (cx + 30 * s, cy + 15 * s)]
    hi, mid, dark = TIERS[t][0], TIERS[t][1], TIERS[t][2]
    return (f'<polygon points="{pts(top)}" fill="{hi}"/><polygon points="{pts(left)}" fill="{mid}"/>'
            f'<polygon points="{pts(right)}" fill="{dark}"/>'
            f'<polygon points="{pts([top[0], top[1], right[3], right[2], left[3], top[3]])}" fill="none" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>'
            f'<polyline points="{pts([top[3], top[2], top[1]])}" fill="none" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>'
            f'<line x1="{f(cx)}" y1="{f(cy + 4 * s)}" x2="{f(cx)}" y2="{f(cy + 30 * s)}" stroke="{INK}" stroke-width="4"/>')


def star_points(cx, cy, r, ri, n=5, rot=-math.pi / 2):
    p = []
    for i in range(n * 2):
        rr = r if i % 2 == 0 else ri
        a = rot + i * math.pi / n
        p.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return p


def star(t, cx, cy, r, stroke=6):
    return (f'<polygon points="{pts(star_points(cx, cy, r, r * 0.47))}" fill="url(#{t}-sym)" stroke="{INK}" '
            f'stroke-width="{stroke}" stroke-linejoin="round"/>')


def hex_gem(t, cx, cy, r):
    outer = [(cx + r * math.cos(-math.pi / 2 + i * math.pi / 3), cy + r * math.sin(-math.pi / 2 + i * math.pi / 3)) for i in range(6)]
    inner = [(cx + r * 0.5 * math.cos(-math.pi / 2 + i * math.pi / 3), cy + r * 0.5 * math.sin(-math.pi / 2 + i * math.pi / 3)) for i in range(6)]
    facets = "".join(f'<line x1="{f(outer[i][0])}" y1="{f(outer[i][1])}" x2="{f(inner[i][0])}" y2="{f(inner[i][1])}" stroke="{INK}" stroke-width="3" stroke-opacity="0.6"/>' for i in range(6))
    return (f'<polygon points="{pts(outer)}" fill="url(#{t}-sym)" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>'
            f'<polygon points="{pts(inner)}" fill="{TIERS[t][0]}" fill-opacity="0.85" stroke="{INK}" stroke-width="3" stroke-opacity="0.6"/>' + facets)


def brilliant(t, cx, cy, s):
    # Side view of a cut diamond: table, crown, pavilion.
    crown_top = cy - 24 * s
    girdle = cy - 6 * s
    tip = cy + 34 * s
    w, tw = 38 * s, 20 * s
    outline = [(cx - tw, crown_top), (cx + tw, crown_top), (cx + w, girdle), (cx, tip), (cx - w, girdle)]
    lines = [((cx - w, girdle), (cx + w, girdle)), ((cx - tw, crown_top), (cx - 10 * s, girdle)), ((cx + tw, crown_top), (cx + 10 * s, girdle)),
             ((cx - 10 * s, girdle), (cx, tip)), ((cx + 10 * s, girdle), (cx, tip)), ((cx, crown_top), (cx - 10 * s, girdle)), ((cx, crown_top), (cx + 10 * s, girdle))]
    l = "".join(f'<line x1="{f(a[0])}" y1="{f(a[1])}" x2="{f(b[0])}" y2="{f(b[1])}" stroke="{INK}" stroke-width="3" stroke-opacity="0.55"/>' for a, b in lines)
    left_face = [(cx - w, girdle), (cx - 10 * s, girdle), (cx, tip)]
    return (f'<polygon points="{pts(outline)}" fill="url(#{t}-sym)"/>'
            f'<polygon points="{pts(left_face)}" fill="#FFFFFF" fill-opacity="0.45"/>' + l +
            f'<polygon points="{pts(outline)}" fill="none" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>')


def crown(t, cx, top, w=96, h=46):
    x0, x1 = cx - w / 2, cx + w / 2
    base = top + h
    p = [(x0, base), (x0 - 4, top + 6), (x0 + w * 0.25, top + h * 0.5), (cx, top - 8), (x1 - w * 0.25, top + h * 0.5), (x1 + 4, top + 6), (x1, base)]
    gems = "".join(f'<circle cx="{f(x)}" cy="{f(y)}" r="6" fill="{TIERS[t][5]}" stroke="{INK}" stroke-width="4"/>'
                   for x, y in [(x0 - 4, top + 4), (cx, top - 10), (x1 + 4, top + 4)])
    return (f'<polygon points="{pts(p)}" fill="url(#{t}-metal)" stroke="{INK}" stroke-width="{OUTLINE}" stroke-linejoin="round"/>'
            f'<rect x="{f(x0 - 2)}" y="{f(base - 12)}" width="{f(w + 4)}" height="14" rx="5" fill="{TIERS[t][2]}" stroke="{INK}" stroke-width="6"/>'
            + gems)


# --- Ribbon with division pips ----------------------------------------------------------------

def ribbon(t, pips, y=206, star_pips=False):
    w = 54 + pips * 22
    x0, x1 = CX - w / 2, CX + w / 2
    tail = [(x0 + 4, y - 4), (x0 - 22, y - 4), (x0 - 12, y + 9), (x0 - 22, y + 22), (x0 + 4, y + 22)]
    tail_r = [(256 - x, yy) for x, yy in tail]
    band = f'<rect x="{f(x0)}" y="{f(y - 12)}" width="{f(w)}" height="30" rx="9" fill="{LINE}" stroke="{INK}" stroke-width="6"/>'
    tails = (f'<polygon points="{pts(tail)}" fill="{TIERS[t][2]}" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>'
             f'<polygon points="{pts(tail_r)}" fill="{TIERS[t][2]}" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>')
    marks = []
    gap = 22
    sx = CX - (pips - 1) * gap / 2
    for i in range(pips):
        x = sx + i * gap
        if star_pips:
            marks.append(f'<polygon points="{pts(star_points(x, y + 3, 10, 4.6))}" fill="{TIERS[t][0]}" stroke="{TIERS[t][1]}" stroke-width="2" stroke-linejoin="round"/>')
        else:
            marks.append(f'<polygon points="{pts([(x, y - 6), (x + 8, y + 3), (x, y + 12), (x - 8, y + 3)])}" fill="{TIERS[t][0]}" stroke="{TIERS[t][1]}" stroke-width="2" stroke-linejoin="round"/>')
    return tails + band + "".join(marks)


# --- Compose ---------------------------------------------------------------------------------

def emblem(tier, division):
    p = TIERS[tier]
    hi, mid, dark = p[0], p[1], p[2]
    outer, inner = frame(tier)
    parts = [defs(tier, p)]

    # Back ornaments grow with the tier, and the third division earns a little extra.
    if tier == "master":
        parts.append(rays(tier, 16, 48, 126))
    elif tier == "diamond":
        parts.append(f'<circle cx="{f(CX)}" cy="{f(CY)}" r="118" fill="url(#{tier}-glow)"/>')
    if tier == "gold":
        parts.append(laurels(tier))
    if division == 3 and tier not in ("diamond", "master"):
        parts.append(f'<circle cx="{f(CX)}" cy="{f(CY)}" r="112" fill="url(#{tier}-glow)"/>')
    if tier == "silver":
        parts.append(wings(tier, 2, 54 + division * 3, 70, CY - 20))
    if tier == "platinum":
        parts.append(wings(tier, 3, 54 + division * 3, 64, CY - 28))
    if tier == "diamond":
        parts.append(wings(tier, 4, 46 + division * 3, 52, CY - 30))
    if tier == "master":
        parts.append(wings(tier, 4, 54, 62, CY - 30))

    # Frame: sticker shadow, metal body, bevel, gloss, outline.
    parts.append(f'<path d="{outer}" transform="translate(0 7)" fill="{INK}" fill-opacity="0.3"/>')
    parts.append(f'<path d="{outer}" fill="none" stroke="#FFF7E8" stroke-width="{OUTLINE + 9}" stroke-linejoin="round"/>')
    parts.append(f'<path d="{outer}" fill="url(#{tier}-metal)" stroke="{INK}" stroke-width="{OUTLINE}" stroke-linejoin="round"/>')
    parts.append(f'<path d="{inner}" transform="translate(0 4)" fill="{hi}" fill-opacity="0.55"/>')
    parts.append(f'<path d="{inner}" fill="url(#{tier}-face)" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>')

    # Symbol.
    if tier == "bronze":
        parts.append(tile_cube(tier, CX, CY - 2, 1.25))
    elif tier == "silver":
        parts.append(tile_cube(tier, CX - 15, CY + 14, 0.8))
        parts.append(tile_cube(tier, CX + 17, CY + 12, 0.8))
        parts.append(tile_cube(tier, CX + 1, CY - 14, 0.8))
    elif tier == "gold":
        parts.append(star(tier, CX, CY + 2, 40))
    elif tier == "platinum":
        parts.append(hex_gem(tier, CX, CY, 40))
    elif tier == "diamond":
        parts.append(brilliant(tier, CX, CY, 1.25))
    else:
        parts.append(star(tier, CX, CY + 4, 42))
        parts.append(f'<circle cx="{f(CX)}" cy="{f(CY + 6)}" r="11" fill="{p[5]}" stroke="{INK}" stroke-width="4"/>')

    # Gloss sweep over the upper-left of the frame.
    parts.append(f'<clipPath id="{tier}-clip"><path d="{inner}"/></clipPath>')
    parts.append(f'<path d="M{f(CX - 60)} {f(CY - 18)} Q{f(CX - 52)} {f(CY - 60)} {f(CX - 6)} {f(CY - 66)}" clip-path="url(#{tier}-clip)" fill="none" stroke="#FFFFFF" '
                 f'stroke-opacity="0.35" stroke-width="9" stroke-linecap="round"/>')

    if tier == "master":
        parts.append(crown(tier, CX, 6, 104, 44))
        parts.append(ribbon(tier, 3, star_pips=True))
    else:
        parts.append(ribbon(tier, division, star_pips=tier in ("gold", "diamond")))

    body = "\n".join(parts)
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 256 256">\n{body}\n</svg>\n'


# --- Podium medals ------------------------------------------------------------------------------

DIGITS = {
    1: "M-10 -22 L4 -32 V32",
    2: "M-18 -18 Q-16 -34 0 -34 Q18 -34 18 -18 Q18 -6 -18 32 H20",
    3: "M-16 -28 Q-6 -36 6 -34 Q20 -30 18 -16 Q16 -4 0 -2 Q20 0 20 14 Q20 34 0 34 Q-12 34 -20 26",
}


def medal(place):
    tier = {1: "gold", 2: "silver", 3: "bronze"}[place]
    p = TIERS[tier]
    cx, cy = 128.0, 150.0
    ribbon_l = [(70, 4), (112, 4), (138, 104), (100, 112)]
    ribbon_r = [(186, 4), (144, 4), (118, 104), (156, 112)]
    parts = [defs(tier, p),
             f'<polygon points="{pts(ribbon_l)}" fill="#75B8FA" stroke="{INK}" stroke-width="7" stroke-linejoin="round"/>',
             f'<polygon points="{pts(ribbon_r)}" fill="#FF7A85" stroke="{INK}" stroke-width="7" stroke-linejoin="round"/>',
             f'<path d="{scalloped_circle(cx, cy, 86, 16, 6)}" transform="translate(0 7)" fill="{INK}" fill-opacity="0.3"/>',
             f'<path d="{scalloped_circle(cx, cy, 86, 16, 6)}" fill="url(#{tier}-metal)" stroke="{INK}" stroke-width="{OUTLINE}" stroke-linejoin="round"/>',
             f'<circle cx="{f(cx)}" cy="{f(cy + 4)}" r="62" fill="{p[0]}" fill-opacity="0.55"/>',
             f'<circle cx="{f(cx)}" cy="{f(cy)}" r="62" fill="url(#{tier}-metal)" stroke="{INK}" stroke-width="6"/>',
             f'<path d="{DIGITS[place]}" transform="translate({f(cx)} {f(cy + 2)}) scale(1.35)" fill="none" stroke="{INK}" stroke-width="11" stroke-linecap="round" stroke-linejoin="round"/>',
             f'<path d="M{f(cx - 54)} {f(cy - 30)} Q{f(cx - 40)} {f(cy - 62)} {f(cx - 4)} {f(cy - 68)}" fill="none" stroke="#FFFFFF" stroke-opacity="0.75" stroke-width="7" stroke-linecap="round"/>']
    body = "\n".join(parts)
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 256 256">\n{body}\n</svg>\n'


for tier in ("bronze", "silver", "gold", "platinum", "diamond"):
    for division in (1, 2, 3):
        (OUT / f"{tier}_{division}.svg").write_text(emblem(tier, division), encoding="utf-8")
(OUT / "master.svg").write_text(emblem("master", 1), encoding="utf-8")
for place in (1, 2, 3):
    (OUT / f"medal_{place}.svg").write_text(medal(place), encoding="utf-8")
print("wrote", len(list(OUT.glob("*.svg"))), "files to", OUT)
