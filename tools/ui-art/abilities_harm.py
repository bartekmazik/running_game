# Harmful/utility ability icons + status emotes, same style as glyphs.py (single white silhouettes).
# Usage: python tools/ui-art/abilities_harm.py
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
    k = r * 0.22
    return solid(f"M{f(cx)} {f(cy - r)} Q{f(cx + k)} {f(cy - k)} {f(cx + r)} {f(cy)} Q{f(cx + k)} {f(cy + k)} {f(cx)} {f(cy + r)} "
                 f"Q{f(cx - k)} {f(cy + k)} {f(cx - r)} {f(cy)} Q{f(cx - k)} {f(cy - k)} {f(cx)} {f(cy - r)} Z")


# ---- local helpers -------------------------------------------------------------------------
def pt(cx, cy, r, deg):
    a = math.radians(deg)
    return cx + r * math.cos(a), cy + r * math.sin(a)


def arc(cx, cy, r, a0, a1, w):
    x0, y0 = pt(cx, cy, r, a0)
    x1, y1 = pt(cx, cy, r, a1)
    large = 1 if (a1 - a0) % 360 > 180 else 0
    return line(f"M{f(x0)} {f(y0)} A{r} {r} 0 {large} 1 {f(x1)} {f(y1)}", w)


def poly(points, round_=3):
    s = " ".join(f"{f(x)},{f(y)}" for x, y in points)
    return f'<polygon points="{s}" fill="{W}" stroke="{W}" stroke-width="{round_ * 2}" stroke-linejoin="round"/>'


def head(tx, ty, deg, length=22, width=28):
    """Arrow head triangle with tip at (tx,ty) pointing at angle deg."""
    a = math.radians(deg)
    dx, dy = math.cos(a), math.sin(a)
    px, py = -dy, dx
    bx, by = tx - dx * length, ty - dy * length
    return poly([(tx - dx * 3, ty - dy * 3), (bx + px * width / 2, by + py * width / 2), (bx - px * width / 2, by - py * width / 2)], 3)


def drop(cx, cy, r):
    return solid(f"M{f(cx)} {f(cy - 2 * r)} Q{f(cx + r)} {f(cy - 0.7 * r)} {f(cx + r)} {f(cy)} "
                 f"A{f(r)} {f(r)} 0 0 1 {f(cx - r)} {f(cy)} Q{f(cx - r)} {f(cy - 0.7 * r)} {f(cx)} {f(cy - 2 * r)} Z")


def spiral(turns, r0, r1, w, cx=64, cy=64, start=200):
    n = int(turns * 40)
    pts = []
    for i in range(n + 1):
        t = i / n
        pts.append(pt(cx, cy, r0 + (r1 - r0) * t, start + 360 * turns * t))
    return line("M" + " L".join(f"{f(x)} {f(y)}" for x, y in pts), w)


def ellipse_pt(cx, cy, rx, ry, deg):
    a = math.radians(deg)
    return cx + rx * math.cos(a), cy + ry * math.sin(a)


def flake(R, w1, w2, branch_at=0.5, branch_len=0.3):
    out = []
    for k in range(3):
        a = math.radians(90 + 60 * k)
        dx, dy = math.cos(a), math.sin(a)
        out.append(line(f"M{f(64 - R * dx)} {f(64 - R * dy)} L{f(64 + R * dx)} {f(64 + R * dy)}", w1))
    for k in range(6):
        a = math.radians(-90 + 60 * k)
        dx, dy = math.cos(a), math.sin(a)
        px, py = -dy, dx
        bx, by = 64 + R * branch_at * dx, 64 + R * branch_at * dy
        for side in (1, -1):
            tx = 64 + R * (branch_at + 0.2) * dx + side * R * branch_len * px
            ty = 64 + R * (branch_at + 0.2) * dy + side * R * branch_len * py
            out.append(line(f"M{f(bx)} {f(by)} L{f(tx)} {f(ty)}", w2))
    return "".join(out)


# ---- abilities -----------------------------------------------------------------------------
def shockwave():
    s = circle(64, 64, 13)
    for base in (0, 90, 180, 270):
        s += arc(64, 64, 30, base - 28 + 45, base + 28 + 45, 10)
        s += arc(64, 64, 50, base - 20, base + 20, 9)
    return s


def sabotage():
    L = [(22, 22), (50, 22), (64, 46), (44, 68), (58, 106), (22, 106)]
    R = [(62, 22), (106, 22), (106, 106), (70, 106), (56, 68), (76, 46)]
    L = [(x - 4, y - 2) for x, y in L]
    R = [(x + 4, y + 2) for x, y in R]
    return poly(L, 4) + poly(R, 4)


def trench():
    return (rrect(14, 24, 36, 62, 9) + rrect(78, 24, 36, 62, 9)
            + line("M64 34 V70", 11) + head(64, 106, 90, 26, 36))


def glue():
    return drop(64, 64, 27) + rrect(14, 100, 100, 14, 7)


def confuse():
    return spiral(2.3, 4, 46, 10)


def blackout():
    eye = line("M10 64 Q64 12 118 64 Q64 116 10 64 Z", 10) + circle(64, 64, 15)
    return eye + line("M26 108 L102 20", 12)


def swap():
    return (line("M24 62 Q36 22 84 38", 11) + head(110, 66, 55, 24, 30)
            + line("M104 66 Q92 106 44 90", 11) + head(18, 62, 235, 24, 30))


def heavyboots():
    # A side-on work boot (shaft, heel, rounded toe) standing on a heavy weight block.
    boot = solid("M34 14 H64 Q70 14 70 20 V50 Q70 56 76 58 L100 66 Q112 70 112 80 V84 Q112 90 106 90 H30 Q24 90 24 84 V24 Q24 14 34 14 Z")
    weight = solid("M20 98 H108 Q114 98 114 104 V110 Q114 116 108 116 H20 Q14 116 14 110 V104 Q14 98 20 98 Z")
    return boot + weight + line("M34 32 H58", 7)

def stun():
    x0, y0 = ellipse_pt(64, 44, 44, 15, 180)
    s = line(f"M{f(x0)} {f(y0)} A44 15 0 1 1 {f(x0 + 0.1)} {f(y0 + 0.1)}", 6)
    s += circle(64, 96, 18)
    for deg in (190, 350, 90):
        x, y = ellipse_pt(64, 44, 44, 15, deg)
        s += star(x, y, 12, 6)
    return s


def catapult():
    # A seesaw launcher: a beam on a triangle, its end flung up, a ball flying off along a dotted arc.
    beam = line("M16 98 L82 64", 11)
    base = solid("M38 112 L52 84 L66 112 Z")
    ball = circle(94, 30, 13)
    dots = circle(82, 52, 5) + circle(70, 64, 4)
    return beam + base + ball + dots + line("M14 112 H90", 8)

def vortex():
    return (rrect(14, 16, 100, 16, 8) + rrect(24, 40, 80, 16, 8) + rrect(36, 64, 56, 16, 8) + rrect(50, 88, 28, 16, 8))


def mine():
    # A flat landmine seen from the side: wide low body, a pressure plate on top, and the blink.
    body = solid("M18 86 Q18 70 34 66 H94 Q110 70 110 86 V94 Q110 104 98 104 H30 Q18 104 18 94 Z")
    plate = rrect(48, 52, 32, 12, 5)
    return body + plate + twinkle(64, 32, 16) + line("M34 86 H94", 6)


ABILITIES = {
    "shockwave": shockwave(), "sabotage": sabotage(), "trench": trench(), "glue": glue(), "confuse": confuse(),
    "blackout": blackout(), "swap": swap(), "heavyboots": heavyboots(), "stun": stun(), "catapult": catapult(),
    "vortex": vortex(), "mine": mine(),
}


def anger():
    s = ""
    for sx in (1, -1):
        for sy in (1, -1):
            def m(x, y):
                return (64 + sx * (x - 64), 64 + sy * (y - 64))
            a, c, b = m(20, 52), m(52, 52), m(52, 20)
            s += line(f"M{f(a[0])} {f(a[1])} Q{f(c[0])} {f(c[1])} {f(b[0])} {f(b[1])}", 12)
    return s


EMOTES = {
    "dizzy": arc(64, 108, 30, 180, 360, 10) + star(26, 62, 11, 5.5) + star(64, 34, 12, 6) + star(102, 62, 11, 5.5),
    "swirl": spiral(2.0, 5, 44, 11),
    "question": line("M40 44 Q40 18 64 18 Q88 18 88 42 Q88 58 72 64 Q64 68 64 80", 14) + circle(64, 104, 10),
    "sleep": line("M18 22 H62 L18 72 H62", 12) + line("M76 70 H104 L76 104 H104", 10),
    "anger": anger(),
    "drops": drop(46, 80, 22) + drop(92, 56, 15) + drop(98, 100, 11),
    "alert": rrect(48, 12, 32, 66, 16) + circle(64, 102, 14),
    "chill": flake(48, 12, 9, 0.5, 0.24),
}


def write(group, items):
    folder = OUT / group
    folder.mkdir(parents=True, exist_ok=True)
    for name, body in items.items():
        (folder / f"{name}.svg").write_text(svg(body), encoding="utf-8")
    return len(items)


n = write("abilities", ABILITIES) + write("emotes", EMOTES)
print("wrote", n, "icons to", OUT)
