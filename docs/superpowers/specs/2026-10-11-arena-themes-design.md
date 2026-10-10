# Arena themes — design

Date: 2026-10-11 · Status: approved in chat, awaiting spec review

## Goal

Turn the arena from eight tinted grids in open sky into a **tower of eight enclosed, themed rooms**.
Each floor is a different place, walled on four sides and closed above, dressed with props and
textures built entirely in code. One coherent palette runs through the arena and the UI, so the
screen stops shouting in eight colours at once.

Success looks like: a screenshot from any floor tells you which floor you are on without the HUD;
looking down a hole shows a different room; nothing on screen competes with the two things that
matter in play — the tile under you and the danger (warning glow, lava).

## Decisions already made (user, 2026-10-11)

| Question | Answer |
|---|---|
| How are props "modelled"? | **Meshes generated in code** (`Mesh` + `Model.Builder`) at startup. No .vmdl files, no external models — same rule as the code-generated textures. |
| Side walls | **Solid, with collision.** Nobody falls off the edge any more; the only way down is a hole. |
| Where props go | **Walls and ceiling only.** The 30×30 play grid stays clear; no obstacles, no floor clutter. |
| Themes, top → bottom | 1 Rooftop greenhouse · 2 Winter · 3 Summer/beach · 4 Office · 5 Library · 6 Workshop · 7 Dungeon · 8 Forge over the lava |
| Kenney | Leaves the floor surfaces (each floor must read as its theme). Stays in the UI and on the in-world floor signs. |
| Order | Mouse bug (closed: hardware — a drifting Xbox pad) → arena → UI. One step per turn, user tests. |

## Constraints carried over from CLAUDE.md

- Tiles keep their **size, position, collider and lifecycle** (disabled, never destroyed;
  arithmetic lookup; batched collapse RPC). Only their *mesh and material* change.
- `Arena` constants are untouched. Interior clear height per floor is therefore
  `FloorGap − TileThickness = 208` units (Citizen is ~72).
- No particle systems, no custom sounds, no downloads. Effects are geometry + emissive material.
- Every generated object is `NotSaved` + `NetworkMode.Never` (as now). The shell is identical on
  every machine because it is deterministic — seeds are constants.
- `ModelRenderer.Tint` stays ≤ 1 per channel (8-bit, wraps). Light colours may exceed 1.
- No static caches of generated resources (hotload keeps statics alive — see memory).

## Palette — one source of truth

New `Code/Art/Palette.cs`, a static class of named colours, used by every arena builder and every
texture painter. `Code/ui/_ui.scss` mirrors the subset the UI needs (SCSS can't read C#, and
`var()` is unsupported), with a comment pointing back to `Palette.cs`.

**Rule:** colour carries meaning. Neutrals for structure, one identity hue per floor, and the
saturated warm family is reserved for danger. Nothing decorative may use the danger family.

| Role | Token | Hex | Use |
|---|---|---|---|
| Neutral | `Paper` | `#F4ECDC` | UI cards, trims, window frames |
| Neutral | `PaperShade` | `#E2D3B8` | UI card edge, shadowed trim |
| Neutral | `Ink` | `#2F2A3B` | Text, outlines, deepest shadow |
| Neutral | `InkSoft` | `#5D5669` | Secondary text, iron, grout |
| Danger | `Warn` | `#FF8A4C` | Tile about to collapse (only) |
| Danger | `Lava` | `#F25C54` | Lava, forge seams (only) |

Floor identity hues — the HUD depth ladder, sign plates and each theme's accent. They sit in one
lightness/chroma band and step from light and cool at the top to darker and warm at the bottom, so
depth reads before hue does.

| Floor | Theme | `FloorHue(n)` |
|---|---|---|
| 1 | Greenhouse | `#A9CF95` sage |
| 2 | Winter | `#A7CBE8` ice |
| 3 | Beach | `#F0CF8A` sand |
| 4 | Office | `#9FB0DA` periwinkle |
| 5 | Library | `#C99BB0` rose |
| 6 | Workshop | `#C9A26B` brass |
| 7 | Dungeon | `#8F86AD` slate lilac |
| 8 | Forge | `#E07E58` ember |

Each theme also defines a small material palette (base, light, shade, accent) in its own theme
file; those values must sit inside the same band. Hex values above are the starting point — final
values are tuned against in-engine screenshots under the real lighting, and the table in this spec
is updated with what shipped.

## Geometry

All dimensions in world units. `S = GridSize × TileSize = 1680`; grid inner edge at `±840`.
Floor `n` tile top at `z = −220n + 6`, underside at `z = −220n − 6`.

**Tiles.** A generated bevelled slab, 56 × 56 × 12, 2-unit chamfer on the top edges. Two
materials: the top and sides wear the floor's surface; the **underside wears the ceiling of the
room below**, so a collapse opens a hole in the ceiling underneath. Four surface variants per
floor (a 2×2 atlas in one texture, picked by a hash of `x,y`) so 900 tiles don't repeat one image.
Tint is white with a ±4% checker, so individual tiles still read when running. The old concentric
bands and the striped edge ring go — with walls, the edge is no longer a danger to be warned of.
The warning state is unchanged in behaviour (emissive override, heat to `Warn`, sag); because the
override covers the underside too, players below see the tile glow in their ceiling before it falls.

**Walls.** One wall segment per side per floor, from that floor's tile underside up to the next
floor's tile underside (220 tall), so the segments stack into one continuous tower. Inner face on
the grid edge; 64 units thick. Collision is one static `BoxCollider` per segment on the inner
face — never a mesh collider. Decoration lives **inside the wall's thickness** (alcoves, shelves,
windows, recessed panels) below 140 units; above 140 units, ceiling props may overhang inward up
to 24 units without collision. Nothing protrudes into the play volume below head height.

**Ceilings.** The ceiling of floor `n` is floor `n−1`'s tile undersides plus a perimeter cornice
and the theme's ceiling props (beams, lamps, girders, stalactites) in the top 60 units.
Floor 1's ceiling is a **glass roof**: a frame lattice resting on the walls at `z = 214`, pitched up 40 units to a ridge, with panes. The frame casts
shadows; panes don't, so the sun still lights the greenhouse in stripes.

**Below floor 8.** The forge walls continue down to the lava (228 units). The lava slab fits the
shaft exactly; its trigger is unchanged.

**Outside.** The outer faces of the walls form a single calm tower facade: `Paper` stone with a
band of windows per floor in that floor's hue, and a cornice line between floors. This is what the
menu orbit shows. The old frame beams, corner posts and their caps are removed.

## Themes

Each theme is one class implementing a shared interface: it paints its textures and emits its
wall, ceiling and prop geometry into the floor's mesh batches. Props are built from the mesh kit's
primitives and are deterministic.

| # | Floor surface | Walls | Ceiling | Props | Light |
|---|---|---|---|---|---|
| 1 | Terracotta tiles, glaze highlights | White-sage glazing bars over planted beds | Glass roof lattice | Planters with leafy plants, hanging ferns, a watering can, potting shelves | Sun through glass + warm fill |
| 2 | Packed snow over ice, sparkle flecks | Log-cabin timber, frosted windows | Pine beams, string of bulb lights | Icicles on the cornice, snow drifts in alcoves, skis, a snowman relief, wreaths | Cool daylight from windows + warm bulbs |
| 3 | Sand with ripples and shells | Painted beach-hut planks | Striped canvas sail | Awnings, surfboards, life ring, palm fronds at the cornice, beach balls in alcoves | Warm, high-key |
| 4 | Carpet tiles, two-tone weave | Partition panels + windows with blinds | Grid ceiling, light panels | Whiteboards, wall clocks, filing cabinets in alcoves, water cooler, potted plant | Neutral-cool panel light |
| 5 | Herringbone parquet | Full-height bookcases | Coffered wood | Books generated with varied height/width/colour, rolling ladder, reading lamps, framed maps | Warm lamplight, chandeliers |
| 6 | Checker-plate steel | Corrugated sheet + riveted posts | Girders | Pipes with valves, pressure gauges, gears, hazard-stripe trim, cage lamps | Cool-white cage lamps |
| 7 | Irregular flagstones, moss in joints | Ashlar stone blocks | Vaulted arches | Torches (emissive flame), chains, barred windows, banners, a skull niche | Low, warm torchlight |
| 8 | Basalt with glowing cracks | Dark rock with lava seams | Stalactites | Anvils, forge mouths with glow, tool racks, bellows | Lava glow from below + forge mouths |

Theme flames and glows are emissive geometry (the matte-copy + full-tint rule from memory),
never particles.

## Textures

New `Code/Art/Painter.cs`: an RGBA float canvas with the primitives the themes paint with —
fills, gradients, rounded rects, circles, lines, value-noise/fbm grain, bevel light/shade — plus a
**height channel** that is turned into a normal map, so bricks, planks and stones have relief under
the lights rather than being flat prints. Output: colour + normal texture pair per surface, applied
to a copy of the matte dev material.

Style target is the user's references (`Assets/gui reference/`): flat colour shapes, a soft
two-tone shade, a little grain, no photographic noise, no pixel art. Surfaces are full colour (no
longer greyscale + tint). Resolution 512² for floor and wall surfaces, 256² for trims and props;
estimated memory under 64 MB for all eight themes.

The current `TileTextures.cs` pixel-art painters and the Kenney floor PNGs in
`Assets/textures/kenney/` become unused and are deleted.

## Lighting and camera

- Floor 1 is lit by the sun through the roof. Floors 2–8 get 4–6 `PointLight`s each, inside their
  lamp props, **no shadows**, in the theme's light colour, plus the lava glow at the bottom.
  Budget: ≤ 48 lights, 0 extra shadow casters.
- The camera boom already traces (`TntPlayer.cs:805`); it must ignore nothing new except the glass
  panes. Walls and cornice block it, so it pulls in instead of clipping through.
- Fog is reduced to a soft depth haze inside; the sky colour is retuned to the palette.

## Code layout

```
Code/Art/Palette.cs            colour tokens + FloorHue(n)
Code/Art/Painter.cs            texture canvas, height → normal, upload to Material
Code/Art/MeshKit.cs            mesh accumulator + primitives (bevel box, cylinder, cone,
                               wedge, arch, sphere, lathe), per-material batches → Model
Code/Arena/ArenaTheme.cs       interface every theme implements
Code/Arena/Themes/*.cs         eight theme classes
Code/Arena/TowerBuilder.cs     walls, collision, ceilings, roof, facade, lights per floor
Code/ArenaGenerator.cs         keeps tiles + lookup + lava; delegates the rest
```

`ArenaGenerator` keeps every public method it has now; `FloorColor(n)` forwards to
`Palette.FloorHue(n)` so the HUD ladder, `ArenaSign` and abilities keep working. `ArenaSign`
moves from corner posts to a plaque on each wall and drops the Kenney floor pattern.

## Build order (one turn each; user tests after each)

0. **Spike** — one generated model with colour + normal texture, a box collider, lit by a point
   light, in play. Settles: `Vertex` field layout, normal-map material parameter name, whether a
   translucent glass material ships with the engine (fallback: open lattice, no panes). Throwaway.
1. Palette, Painter, MeshKit; themed tile models with ceiling undersides; floor textures for all
   eight themes; `_ui.scss` floor tokens updated.
2. Tower shell: walls with collision, cornices, glass roof, facade, lights, lava shaft; frame,
   posts, edge ring and clouds-inside-the-tower removed; signs moved to walls.
3. Props: floors 1–2. 4. Floors 3–4. 5. Floors 5–6. 6. Floors 7–8.
7. Menu orbit framing of the new facade.
8. UI pass (separate design round): palette, information hierarchy.

## Verification

Per step: `compile_status` clean over the editor MCP, then in-engine screenshots from fixed
viewpoints on every floor touched (standing, looking along a wall, looking up, looking down a hole),
compared against the references. No bots or second instances — gameplay testing is the user's.
Frame time checked once after step 2 and once after step 6.

## Risks

- `Vertex` layout and normal-map parameter names are undocumented in the skill → the spike.
- No shipped glass material → open-lattice roof (still "closed from above" in silhouette; decided
  with the user if it comes to that).
- 4–6 unshadowed lights per floor may look flat → bake contact shade into textures and keep AO.
- Ability effects that assume an open edge (launch, hook) now hit walls; behaviour is physically
  correct, but the user's test pass should cover the edge abilities.
