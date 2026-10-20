# UI pass — art guide (2026-10-20)

One visual language for every icon, emblem and ornament in TNT Run. The game is a storybook /
toy-box arcade: cream paper cards, plum ink instead of black, chunky pastel "toy bricks" with a
darker bottom edge. Icons are **stickers**: bold rounded shapes, one thick plum outline, flat
pastel fills with one shade and one highlight. Think Kenney / Animal Crossing / Fall Guys UI —
friendly, readable at 40 px, never thin line-art, never photoreal.

Reference images: `Assets/gui reference/*` (storybook cards, sparkles, ribbons, star badges).

## Hard technical rules (the renderer is Svg.Skia inside s&box)

- Plain SVG 1.1, presentation attributes only. **No `<style>`, no CSS classes, no `<text>`, no
  `<filter>`, no `<foreignObject>`, no `<image>`, no external refs, no scripts.**
- Allowed: `path`, `rect`, `circle`, `ellipse`, `polygon`, `polyline`, `line`, `g` with
  `transform`, `opacity`, `fill-opacity`, `stroke-*`, `linearGradient`, `radialGradient` (in
  `<defs>`, referenced as `fill="url(#id)"`; ids unique within the file, prefixed by file name).
- Root: `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 128 128">`
  for icons; emblems use `width="512" height="512" viewBox="0 0 256 256"`.
- Keep files small (< 6 KB icons). Round coordinates to 1 decimal.
- Everything inside the safe area: icons 10..118 of the 128 box, including outline and shadow.

## Palette (only these)

| token | hex | use |
|---|---|---|
| ink | `#3D2E5C` | every outline, motion lines, dark details |
| night | `#2B2142` | deepest shadow only |
| paper | `#FFF7E8` | main light fill |
| paper-deep | `#F7E8CC` | shade on paper |
| butter | `#FFCC61` / deep `#E2A635` | gold, energy, speed |
| mint | `#7ADB94` / deep `#48B067` | (Help bricks are mint — avoid as main fill on Help icons) |
| coral | `#FF7A85` / deep `#DD5361` | (Harm bricks are coral — avoid as main fill on Harm icons) |
| sky | `#75B8FA` / deep `#4C8FD6` | air, ice, water, magic |
| lilac | `#D18AEB` / deep `#A862C4` | magic, confusion, time |
| peach | `#FFA375` / deep `#E07C4C` | warmth, fire (not lava) |
| white | `#FFFFFF` | highlights only |
| danger | `#FF8A4C` warn, `#EB3861` lava | **reserved**: only TNT/explosion/fire danger cues |

## Sticker construction (every ability icon and glyph)

1. **Drop shadow**: the main silhouette repeated first, translated `(0, 5)`, `fill="#2B2142"
   fill-opacity="0.28"`, no stroke. It sits the sticker on the brick.
2. **Body**: main shapes, flat fills from the palette, `stroke="#3D2E5C" stroke-width="7"
   stroke-linejoin="round" stroke-linecap="round"`. Outline weight is the same on every icon.
   Secondary inner lines: `stroke-width="5"`.
3. **Shade**: one shape inside the body (lower/right side), fill = the body colour's deep
   variant, no stroke, so the outline stays on top (draw shade before the outlined body using a
   clipped duplicate, or draw it after the body with the stroke repeated on top — either is fine,
   just keep the outline visible and unbroken).
4. **Highlight**: one short white stroke or small ellipse at upper-left, `opacity 0.75`.
5. **Motion / effect lines**: ink, stroke-width 6–7, round caps, 2–3 of them, never more.

Readable at 40 px means: one idea per icon, 1–3 main shapes, no detail smaller than ~8 units.
Colour choice: the icon sits on a **mint** brick (Help) or **coral** brick (Harm), and on cream
paper in the loadout. Main fills should be paper / butter / sky / lilac / peach so they pop on both.

## Rank emblems (separate set, richer)

Metal gradients allowed, 256 box, outline `#2B2142` 8 units. Each tier has its own silhouette;
divisions I–III add pips and ornament. See `Code/Profile/Ranks.cs` for tier colours.

## Exemplars

`Assets/ui/icons/abilities/dash.svg` and `shove.svg` are the reference stickers. Match their
outline weight, shadow and highlight exactly.

## Verify

`python tools/ui-art/sheet.py <out.png> <folder>` renders every SVG on
mint / coral / night / paper at 40, 104 and 200 px with headless Chrome. Look at the PNG; fix
anything that doesn't read at 40 px or breaks the rules above.
