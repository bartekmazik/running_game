# Contact sheet for the UI-pass SVGs: renders every SVG in the given folders with headless Chrome
# on the backgrounds the game puts them on, at the sizes the game shows them.
# Usage: python sheet.py <out.png> <folder> [<folder> ...] [--sizes 40,104,200] [--bg mint,coral,night,paper]
import sys, os, subprocess, pathlib, html

CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
BG = {
    "mint": "#7ADB94", "coral": "#FF7A85", "night": "#2B2142", "paper": "#FFF7E8",
    "sky": "#75B8FA", "butter": "#FFCC61", "photo": "linear-gradient(135deg,#BFD8E8,#8a6a4a)",
}

args = sys.argv[1:]
out = os.path.abspath(args.pop(0))
sizes = [40, 104, 200]
bgs = ["mint", "coral", "night", "paper"]
folders = []
while args:
    a = args.pop(0)
    if a == "--sizes": sizes = [int(x) for x in args.pop(0).split(",")]
    elif a == "--bg": bgs = args.pop(0).split(",")
    else: folders.append(a)

files = []
for f in folders:
    p = pathlib.Path(f)
    files += sorted(p.glob("*.svg")) if p.is_dir() else [p]

rows = []
for f in files:
    uri = f.resolve().as_uri()
    cells = []
    for bg in bgs:
        for s in sizes:
            r = max(6, s // 4)
            cells.append(f'<div class="c" style="background:{BG[bg]};width:{s+16}px;height:{s+16}px;border-radius:{r}px">'
                         f'<img src="{uri}" width="{s}" height="{s}"></div>')
    rows.append(f'<div class="r"><div class="n">{html.escape(f.stem)}</div>{"".join(cells)}</div>')

width = 150 + len(bgs) * sum(s + 24 for s in sizes) + 40
height = 40 + len(files) * (max(sizes) + 32)
page = f"""<html><body style="margin:0;padding:20px;background:#d9cfe3;font:14px sans-serif;color:#3D2E5C">
<style>.r{{display:flex;align-items:center;gap:8px;margin-bottom:12px}}.n{{width:140px;font-weight:bold}}
.c{{display:flex;align-items:center;justify-content:center}}</style>{''.join(rows)}</body></html>"""
tmp = out.replace(".png", ".html")
open(tmp, "w", encoding="utf-8").write(page)
subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files",
                f"--screenshot={out}", f"--window-size={width},{height}", pathlib.Path(tmp).as_uri()],
               check=True, capture_output=True, timeout=120)
print("saved", out, f"({len(files)} svgs)")
