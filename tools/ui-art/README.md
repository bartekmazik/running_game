# UI art (v2, "storybook")

Everything here is generated from code. Run a script from the project root and it rewrites its SVGs.

| Script | Writes | What |
|---|---|---|
| `glyphs.py` | `Assets/ui/icons/glyphs`, six of `Assets/ui/icons/abilities` | UI glyphs + the exemplar ability icons |
| `abilities_help.py` | `Assets/ui/icons/abilities` | the twelve other Help abilities |
| `abilities_harm.py` | `Assets/ui/icons/abilities`, `Assets/ui/icons/emotes` | the twelve other Harm abilities, nameplate emotes |
| `ranks.py` | `Assets/ui/ranks` | 16 rank emblems + 3 podium medals |
| `sheet.py` | a PNG you name | contact sheet of SVGs on game colours, via headless Chrome |

## The look

Matches `Assets/gui reference/`: muted and quiet.

- **Glyphs and ability icons:** single-colour white silhouettes, 128 box, live area 14..114. Line
  weight 12 (secondary 8), round caps; holes cut with evenodd. No outline, no shadow, no gradient.
  The UI colours them with `background-image-tint`. Ability icons sit on a sage (Help) or terracotta
  (Harm) rounded tile.
- **Rank emblems:** one pennant silhouette. Tier = colour + motif (tile, two tiles, star, gem,
  diamond, crown); division = 1-3 chevrons. Flat fill, soft top-light gradient, thin keyline, a low
  contact shadow. Nothing else.
- **Palette** lives in `Code/UI/_ui.scss`: paper `#F4ECDA`, ink `#4A4039`, slate `#343F3C`, sage,
  terracotta, mustard, dusty blue. Headings are small spaced capitals; write the casing in the markup.
- One ornament per title at most (the small sparkle in `.title-row`). No rays, ribbons or confetti.

Check new art with `python tools/ui-art/sheet.py out.png <folder> --sizes 24,48,104 --bg sage,terra,slate`.
