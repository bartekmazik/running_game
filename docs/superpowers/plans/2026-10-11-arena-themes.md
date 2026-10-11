# Arena Themes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the open, tinted test grids with a tower of eight enclosed, themed rooms whose geometry and textures are generated in code, under one palette shared with the UI.

**Architecture:** Three art primitives — `Palette` (colour tokens), `Painter` (code-painted colour + height → normal textures), `MeshKit` (procedural mesh batches → `Model`) — feed eight `ArenaTheme` classes. `ArenaGenerator` keeps tiles, lookup and lava and builds themed tile models; a new `TowerBuilder` builds walls with box collision, ceilings, roof, facade and lights per floor by asking each theme to dress its room.

**Tech Stack:** s&box (engine 26.09.29), C# components, `Sandbox.Mesh` / `Model.Builder` / `Texture.Create` / `Material.CreateCopy`. No test framework exists for this project: verification is the editor's compiler over MCP (`compile_status`) plus in-engine screenshots from fixed viewpoints.

**Spec:** `docs/superpowers/specs/2026-10-11-arena-themes-design.md`

## Global Constraints

- `Arena` constants unchanged: GridSize 30, TileSize 56, TileThickness 12, FloorCount 8, FloorGap 220, LavaOffset 250.
- Tiles keep position, 56×56×12 box collision, `Enabled=false` lifecycle, arithmetic lookup, batched collapse RPC.
- Everything generated is `GameObjectFlags.NotSaved` + `NetworkMode.Never`, deterministic from constant seeds.
- `ModelRenderer.Tint` ≤ 1 per channel. `Light.LightColor` may exceed 1.
- No static caches of generated `Material`/`Texture`/`Model` (hotload keeps statics alive).
- No particle systems, no files from outside, no .vmdl authoring. Effects = emissive geometry.
- Danger colours (`Palette.Warn`, `Palette.Lava`) are used for danger only.
- Walls: inner face on the grid edge (±840), 64 thick, collision = one static `BoxCollider` per side per floor. Nothing visible protrudes into the play volume below 140 units above the tile top; above 140, overhang ≤ 24 units.
- Light budget: ≤ 6 `PointLight` per floor, `Shadows = false`.
- One step per user turn; stop after each numbered checkpoint for the user's test.

## Review Focus

1. **Falling down a hole next to a wall** — the player must drop cleanly; wall colliders must not overlap the edge tile column (collider inner face exactly at ±840, never inside). Pinned in Task 6 step "edge drop check".
2. **Camera against walls and low ceilings** — boom pulls in, never shows the outside of the tower or the inside of a wall. Pinned in Task 6 screenshots "edge camera".
3. **Round reset** — `ResetAll` must restore themed tiles (variant model + material) after warnings swapped the material. Pinned in Task 5 step "reset check".
4. **Late joiner / client** — themed geometry must be identical on every machine (deterministic seeds, no `Random.Shared`). Pinned in Task 2 and Task 3 by seeding every `System.Random` from a constant.
5. **Abilities that launch players sideways** (Launch, Hook) — now collide with walls; they must not tunnel through a 64-unit wall. Pinned in Task 6 by using a solid box (not a plane) for the wall collider.

---

## File structure

```
Code/Art/Palette.cs          colour tokens + FloorHue(n)                         (create)
Code/Art/Painter.cs          canvas: SDF shapes, grain, height → normal, material (create)
Code/Art/MeshKit.cs          Skin + mesh batches: Quad/Tri/Box/BevelBox/Lathe     (create)
Code/Arena/ArenaTheme.cs     abstract theme + FloorContext (wall-space helpers)   (create)
Code/Arena/Themes/GreenhouseTheme.cs … ForgeTheme.cs   eight themes             (create)
Code/Arena/TowerBuilder.cs   walls, colliders, facade, lava shaft, lights        (create)
Code/ArenaGenerator.cs       tiles use themed models; frame/edge ring removed      (modify)
Code/ui/_ui.scss             $floor0..7 mirror Palette.FloorHue                 (modify)
Code/ui/ArenaSign.razor(.scss)  plaque on walls, no Kenney floor pattern          (modify)
Code/TileTextures.cs         deleted at the end (Task 11)                          (delete)
Assets/textures/kenney/tile_floor*.png, tile_edge.png, frame_bricks.png          (delete, Task 11)
```

---

### Task 0: Spike — generated model, textures, normal encoding, glass (THROWAWAY)

**Files:**
- Create (temporary): `Code/Art/SpikeProbe.cs`
- Modify (temporary): `Code/RoundManager.cs` — one `AddComponent<SpikeProbe>()` line in `OnAwake`

**Answers this task must produce** (write them into this plan under "Spike findings" and into memory `sbox-procedural-mesh`):
1. Front-face winding for `Mesh` index order (CCW or CW seen from the front).
2. `Vertex.Layout` usage: `mesh.CreateVertexBuffer( count, Vertex.Layout, list )` compiles and renders.
3. Normal map: parameter name (`"Normal"` vs `"g_tNormal"`) and encoding (plain tangent-space RGB vs hemi-octahedral RG + roughness B — the compiled dev normals say `HemiOctIsoRoughness_RG_B`), and the green-channel direction.
4. Whether `material.SetFeature( "F_TRANSLUCENT", 1 )` + `g_flOpacityScale` on a complex-shader copy renders see-through.
5. Whether `Mesh.Bounds` is settable (Cecil shows only a property name).

- [ ] **Step 1: Write the probe**

```csharp
using System;
using System.Collections.Generic;

/// THROWAWAY spike for docs/superpowers/plans/2026-10-11-arena-themes.md Task 0. Delete after.
public sealed class SpikeProbe : Component
{
	protected override void OnStart()
	{
		var origin = new Vector3( 0f, 0f, 120f );

		// Four panels in a row, each 80x80, facing -X (towards a camera placed at -X looking +X).
		Panel( origin + new Vector3( 0f, -150f, 0f ), "rgb_Normal", Encode.Rgb, "Normal" );
		Panel( origin + new Vector3( 0f, -50f, 0f ), "rgb_g_tNormal", Encode.Rgb, "g_tNormal" );
		Panel( origin + new Vector3( 0f, 50f, 0f ), "hemi_Normal", Encode.HemiOct, "Normal" );
		Panel( origin + new Vector3( 0f, 150f, 0f ), "glass", Encode.None, null, translucent: true );

		var light = new GameObject( GameObject, true, "Spike Light" );
		light.WorldPosition = origin + new Vector3( -120f, -60f, 80f );
		var point = light.AddComponent<PointLight>();
		point.LightColor = new Color( 3f, 2.8f, 2.6f );
		point.Radius = 600f;
		point.Shadows = false;
	}

	enum Encode { None, Rgb, HemiOct }

	void Panel( Vector3 center, string name, Encode encode, string normalParam, bool translucent = false )
	{
		const int Size = 128;
		var color = new byte[Size * Size * 4];
		var normal = new byte[Size * Size * 4];

		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
		{
			// Four raised bricks; height 1 inside, 0 in the mortar, a 6px slope between.
			float H( int px, int py )
			{
				px = (px + Size) % Size; py = (py + Size) % Size;
				var cx = px % 64; var cy = py % 64;
				var d = MathF.Min( MathF.Min( cx, 63 - cx ), MathF.Min( cy, 63 - cy ) );
				return Math.Clamp( (d - 4f) / 6f, 0f, 1f );
			}

			var dx = (H( x + 1, y ) - H( x - 1, y )) * 4f;
			var dy = (H( x, y + 1 ) - H( x, y - 1 )) * 4f;
			var n = new Vector3( -dx, -dy, 1f ).Normal;

			var i = (y * Size + x) * 4;
			color[i] = 230; color[i + 1] = 200; color[i + 2] = 170; color[i + 3] = translucent ? (byte)90 : (byte)255;

			if ( encode == Encode.Rgb )
			{
				normal[i] = (byte)((n.x * 0.5f + 0.5f) * 255f);
				normal[i + 1] = (byte)((n.y * 0.5f + 0.5f) * 255f);
				normal[i + 2] = (byte)((n.z * 0.5f + 0.5f) * 255f);
			}
			else
			{
				// Hemi-octahedral: project onto the octahedron, rotate 45 degrees, store in RG.
				var sum = MathF.Abs( n.x ) + MathF.Abs( n.y ) + n.z;
				var ox = n.x / sum; var oy = n.y / sum;
				var hx = ox + oy; var hy = ox - oy;
				normal[i] = (byte)((hx * 0.5f + 0.5f) * 255f);
				normal[i + 1] = (byte)((hy * 0.5f + 0.5f) * 255f);
				normal[i + 2] = 200; // roughness ~0.8
			}
			normal[i + 3] = 255;
		}

		var colorTex = Texture.Create( Size, Size, ImageFormat.RGBA8888 ).WithName( name + "_c" ).WithData( color ).Finish();
		var material = Material.Load( "materials/dev/dev_nonmetal_rough70.vmat" ).CreateCopy( "spike_" + name );
		material.Set( "Color", colorTex );
		material.Set( "g_flModelTintAmount", 1f );

		if ( encode != Encode.None )
		{
			var normalTex = Texture.Create( Size, Size, ImageFormat.RGBA8888 ).WithName( name + "_n" ).WithData( normal ).Finish();
			material.Set( normalParam, normalTex );
		}

		if ( translucent )
		{
			material.SetFeature( "F_TRANSLUCENT", 1 );
			material.Set( "g_flOpacityScale", 0.4f );
		}

		// One quad facing -X, wound CCW as seen from -X, plus a back copy wound the other way,
		// labelled so the screenshot shows which winding is the front.
		var verts = new List<Vertex>();
		var h = 40f;
		Vector3 a = center + new Vector3( 0f, h, -h ), b = center + new Vector3( 0f, -h, -h ),
			c = center + new Vector3( 0f, -h, h ), d = center + new Vector3( 0f, h, h );
		var nrm = new Vector3( -1f, 0f, 0f );
		var tan = new Vector3( 0f, -1f, 0f );
		verts.Add( new Vertex( a, nrm, tan, new Vector4( 0f, 1f, 0f, 0f ) ) );
		verts.Add( new Vertex( b, nrm, tan, new Vector4( 1f, 1f, 0f, 0f ) ) );
		verts.Add( new Vertex( c, nrm, tan, new Vector4( 1f, 0f, 0f, 0f ) ) );
		verts.Add( new Vertex( d, nrm, tan, new Vector4( 0f, 0f, 0f, 0f ) ) );
		var indices = new List<int> { 0, 1, 2, 0, 2, 3 };

		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer( verts.Count, Vertex.Layout, verts );
		mesh.CreateIndexBuffer( indices.Count, indices );
		mesh.Bounds = BBox.FromPositionAndSize( center, 100f );

		var model = Model.Builder.WithName( "spike_" + name ).AddMesh( mesh ).Create();
		var go = new GameObject( GameObject, true, "Spike " + name );
		go.AddComponent<ModelRenderer>().Model = model;
		var box = go.AddComponent<BoxCollider>();
		box.Center = center;
		box.Scale = new Vector3( 4f, 80f, 80f );
		box.Static = true;
	}
}
```

- [ ] **Step 2: Compile** — save, then `python t.py compile_status '{}'` (helper in the scratchpad; see memory `sbox-editor-mcp`). Expected: 0 errors. If `mesh.Bounds =` fails to compile, delete that line and record "Bounds is read-only; ModelBuilder.WithViewBounds instead".

- [ ] **Step 3: Look** — `play_stop`, `play_start`, then place the camera with `console_command` / scene tools at (−260, 0, 120) looking +X and `camera_screenshot`. Then from (+260, 0, 120) looking −X.
  Record: which panels are visible from −X (front winding), which normal panel shows four lit bricks with light from the upper-left (correct param + encoding + green direction), whether the glass panel is see-through.

- [ ] **Step 4: Iterate only if needed** — if no normal panel shows relief, try `normal[i+1]` inverted (green flip) on the RGB panels, recompile, look again. Max two iterations, then record "no normal maps; bake shade into colour".

- [ ] **Step 5: Record and remove** — write findings into this plan below, save memory `sbox-procedural-mesh` (type reference) with the five answers, delete `SpikeProbe.cs` and the `AddComponent` line, confirm `compile_status` clean. No commit of the probe.

**Spike findings:** (filled in by Step 5 — 2026-10-11, editor 26.09.29; evidence in `.superpowers/sdd/2026-10-11-arena-themes/task-0-report.md`)

1. **Winding: CCW seen from the front.** Indices `0,1,2, 0,2,3` over a=bottom-left, b=bottom-right,
   c=top-right, d=top-left (as seen by the viewer) render from −X and are back-face culled from +X
   (nothing visible). Single-sided; a two-sided wall needs a second, reversed quad.
2. **`Vertex.Layout`: compiles and renders, but is obsolete** (CS0618 "use CreateVertexBuffer without
   layout"). Use the non-obsolete overload instead — verified rendering identically:
   `mesh.CreateVertexBuffer( verts.Count, verts );` (`List<Vertex>`). Vertex ctor used:
   `new Vertex( pos, normal, tangentAsVector3 /* = +U direction */, new Vector4( u, v, 0, 0 ) )`,
   V grows downward (v=0 at the top of the image).
3. **Normal map: hemi-octahedral, tangent-space +Y = image UP, either param name.**
   - `material.Set( "Normal", tex )` and `material.Set( "g_tNormal", tex )` behave identically.
   - The shader always decodes hemi-oct RG: plain RGB data comes out rotated 45° (top bevels lit
     from the upper-left etc.), so RGB is wrong.
   - Green direction: with `dy` = height difference along image rows (row+1 minus row−1, i.e. down),
     the normal is `new Vector3( -dx, +dy, 1f ).Normal` — the brief's `-dy` lit the bricks upside down.
   - Working encode (RGBA8888, B = roughness byte, only 200 ≈ 0.8 tried — not verified that B drives
     roughness):
     ```csharp
     var n = new Vector3( -dx, dy, 1f ).Normal;
     var sum = MathF.Abs( n.x ) + MathF.Abs( n.y ) + n.z;
     var ox = n.x / sum; var oy = n.y / sum;
     target[o + 0] = (byte)(((ox + oy) * 0.5f + 0.5f) * 255f);
     target[o + 1] = (byte)(((ox - oy) * 0.5f + 0.5f) * 255f);
     target[o + 2] = 200; // roughness
     target[o + 3] = 255;
     ```
   - For `Painter`: change the caller to `EncodeNormal( new Vector3( -dx, dy, 1f ).Normal, ... )` and
     replace the RGB body of `EncodeNormal` with the hemi-oct body above.
4. **Glass: not usable.** `SetFeature( "F_TRANSLUCENT", 1 )` on a `dev_nonmetal_rough70` copy (with or
   without `g_flOpacityScale = 0.4`) does turn on alpha blending (texture alpha × opacity) — opaque
   geometry drawn *before* it shows through tinted. But it is not ordered after opaque geometry: the
   floor tiles, the cloud ring and some panels behind it paint straight over it, so it reads as a
   hole/grey card, not a pane. A copy of the shipped `materials/dev/primary_white_trans.vmat` (complex,
   F_TRANSLUCENT compiled in) + `g_flOpacityScale` showed the same floor-overwrite. `g_flOpacityScale`
   alone: fully opaque. **Decision: no translucent panes — build glass as opaque painted
   "sky-tinted" panels or leave the frame open** (Task 6 roof: open lattice or opaque panes).
5. **`Mesh.Bounds` is settable** — `mesh.Bounds = BBox.FromPositionAndSize( center, size );` compiles and
   the meshes render (no culling glitches seen). `ModelBuilder.WithViewBounds( BBox )` also exists.

---

### Task 1: Palette (single source of colour truth)

**Files:**
- Create: `Code/Art/Palette.cs`
- Modify: `Code/ArenaGenerator.cs:65-75` (`FloorColor` forwards to `Palette.FloorHue`), `Code/ArenaGenerator.cs:84` (`WarnColor` = `Palette.Warn`)
- Modify: `Code/ui/_ui.scss` (`$floor0..$floor7`, add `$paper`, `$paper-shade`, `$ink`, `$ink-soft`, `$warn`, `$lava` if missing — keep existing variable names that other sheets use)

**Interfaces — Produces:**
- `public static class Palette` with `public static Color Paper, PaperShade, Ink, InkSoft, Warn, Lava` (properties, not static fields — hotload) and `public static Color FloorHue( int floor )`, `public static Color Hex( uint rgb )`.

- [ ] **Step 1: Write `Palette.cs`**

```csharp
/// <summary>
/// The colours of the game, in one place. Arena builders, texture painters and (mirrored by hand in
/// Code/ui/_ui.scss) the UI all read from here, so a hue can't drift between the HUD and the world.
///
/// Rule: neutrals carry structure, one identity hue per floor, and the warm saturated pair Warn/Lava
/// means danger — nothing decorative may use them.
///
/// Expression-bodied properties rather than static fields: hotload keeps statics alive, and an edited
/// colour would otherwise not show until the editor restarts.
/// </summary>
public static class Palette
{
	public static Color Hex( uint rgb ) => Color.FromBytes( (int)(rgb >> 16) & 0xFF, (int)(rgb >> 8) & 0xFF, (int)rgb & 0xFF, 255 );

	public static Color Paper => Hex( 0xF4ECDC );
	public static Color PaperShade => Hex( 0xE2D3B8 );
	public static Color Ink => Hex( 0x2F2A3B );
	public static Color InkSoft => Hex( 0x5D5669 );

	public static Color Warn => Hex( 0xFF8A4C );
	public static Color Lava => Hex( 0xF25C54 );

	/// <summary>
	/// Identity hue per floor, top (0) to bottom (7): light and cool at the top, darker and warm near
	/// the lava, all in one lightness/chroma band. Mirrored as $floor0..$floor7 in _ui.scss.
	/// </summary>
	public static Color FloorHue( int floor ) => floor switch
	{
		0 => Hex( 0xA9CF95 ), // greenhouse sage
		1 => Hex( 0xA7CBE8 ), // winter ice
		2 => Hex( 0xF0CF8A ), // beach sand
		3 => Hex( 0x9FB0DA ), // office periwinkle
		4 => Hex( 0xC99BB0 ), // library rose
		5 => Hex( 0xC9A26B ), // workshop brass
		6 => Hex( 0x8F86AD ), // dungeon slate lilac
		_ => Hex( 0xE07E58 ), // forge ember
	};
}
```

- [ ] **Step 2: Forward the old API** — in `ArenaGenerator.cs` replace the body of `FloorColor` with `=> Palette.FloorHue( floor );` (keep its doc comment's first line, update the rest to point at `Palette`), and `WarnColor` with `public static Color WarnColor => Palette.Warn;`.

- [ ] **Step 3: Mirror in SCSS** — in `_ui.scss` set `$floor0: #A9CF95; $floor1: #A7CBE8; $floor2: #F0CF8A; $floor3: #9FB0DA; $floor4: #C99BB0; $floor5: #C9A26B; $floor6: #8F86AD; $floor7: #E07E58;` with a comment `// Mirror of Palette.FloorHue (Code/Art/Palette.cs) — change both together.`

- [ ] **Step 4: Compile** — `compile_status`: 0 errors.

- [ ] **Step 5: Commit** — `git add Code/Art/Palette.cs Code/ArenaGenerator.cs Code/ui/_ui.scss` · message `Palette: one source for floor hues and danger colours`.

---

### Task 2: Painter (code-painted textures with relief)

**Files:**
- Create: `Code/Art/Painter.cs`

**Interfaces — Consumes:** spike findings 3 (normal param + encoding). **Produces:**
- `public sealed class Painter( int size, Color fill )`; `int Size`
- `void Shape( Func<float,float,float> sdf, float minX, float minY, float maxX, float maxY, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f )` — signed-distance fill, 1-px anti-aliased edge, wraps across edges; if `height` is a number, writes height with a rounded `bevel`-px shoulder.
- `void Rect( float x, float y, float w, float h, Color color, float radius = 0f, float alpha = 1f, float height = float.NaN, float bevel = 0f )`
- `void Circle( float cx, float cy, float r, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f )`
- `void Line( Vector2 a, Vector2 b, float width, Color color, float alpha = 1f, float height = float.NaN )`
- `void Mottle( float amount, float scale, int seed )` — tileable value-noise fbm brightness variation
- `void Grain( float amount, int seed )` — per-pixel luminance noise
- `void HeightNoise( float amount, float scale, int seed )`
- `void ShadeFromHeight( float light, float shadow )` — bakes a soft two-tone from the height slope (upper-left lit) into colour; the painterly look
- `Material ToMaterial( string name, string baseMaterial, float normalStrength )` — colour + normal textures on a copy of `baseMaterial`, tint amount 1
- `public const string Matte = "materials/dev/dev_nonmetal_rough70.vmat"`, `public const string Metal = "materials/dev/dev_metal_rough50.vmat"`

- [ ] **Step 1: Write `Painter.cs`** (normal encoding branch per spike finding 3; shown here with plain RGB — swap the body of `EncodeNormal` if the spike chose hemi-oct)

```csharp
using System;

/// <summary>
/// A small canvas for painting surfaces in code: colour plus a height channel that becomes a normal
/// map, so bricks and planks catch the light instead of being flat prints. Everything wraps at the
/// edges, so any surface painted here tiles seamlessly.
///
/// The style it is built for is the user's references: flat colour shapes, a soft two-tone shade,
/// a little grain — no photographic noise and no pixel art.
/// </summary>
public sealed class Painter
{
	public const string Matte = "materials/dev/dev_nonmetal_rough70.vmat";
	public const string Metal = "materials/dev/dev_metal_rough50.vmat";

	public int Size { get; }

	readonly Vector3[] _color;
	readonly float[] _height;

	public Painter( int size, Color fill )
	{
		Size = size;
		_color = new Vector3[size * size];
		_height = new float[size * size];
		Array.Fill( _color, new Vector3( fill.r, fill.g, fill.b ) );
	}

	int Wrap( int v ) => ((v % Size) + Size) % Size;
	int Index( int x, int y ) => Wrap( y ) * Size + Wrap( x );

	public void Shape( Func<float, float, float> sdf, float minX, float minY, float maxX, float maxY, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f )
	{
		var target = new Vector3( color.r, color.g, color.b );

		for ( var y = (int)MathF.Floor( minY ) - 1; y <= (int)MathF.Ceiling( maxY ) + 1; y++ )
		for ( var x = (int)MathF.Floor( minX ) - 1; x <= (int)MathF.Ceiling( maxX ) + 1; x++ )
		{
			var d = sdf( x + 0.5f, y + 0.5f );
			var coverage = Math.Clamp( 0.5f - d, 0f, 1f );
			if ( coverage <= 0f ) continue;

			var i = Index( x, y );
			_color[i] = Vector3.Lerp( _color[i], target, coverage * alpha );

			if ( float.IsNaN( height ) ) continue;
			var shoulder = bevel > 0f ? Smooth( Math.Clamp( -d / bevel, 0f, 1f ) ) : 1f;
			_height[i] = MathX.Lerp( _height[i], height * shoulder, coverage );
		}
	}

	static float Smooth( float t ) => t * t * (3f - 2f * t);

	public void Rect( float x, float y, float w, float h, Color color, float radius = 0f, float alpha = 1f, float height = float.NaN, float bevel = 0f )
	{
		var cx = x + w * 0.5f; var cy = y + h * 0.5f;
		var hx = w * 0.5f - radius; var hy = h * 0.5f - radius;
		Shape( ( px, py ) =>
		{
			var qx = MathF.Abs( px - cx ) - hx; var qy = MathF.Abs( py - cy ) - hy;
			var outside = new Vector2( MathF.Max( qx, 0f ), MathF.Max( qy, 0f ) ).Length;
			return outside + MathF.Min( MathF.Max( qx, qy ), 0f ) - radius;
		}, x, y, x + w, y + h, color, alpha, height, bevel );
	}

	public void Circle( float cx, float cy, float r, Color color, float alpha = 1f, float height = float.NaN, float bevel = 0f )
	{
		Shape( ( px, py ) => new Vector2( px - cx, py - cy ).Length - r, cx - r, cy - r, cx + r, cy + r, color, alpha, height, bevel );
	}

	public void Line( Vector2 a, Vector2 b, float width, Color color, float alpha = 1f, float height = float.NaN )
	{
		var ab = b - a;
		var lengthSq = MathF.Max( ab.LengthSquared, 0.0001f );
		Shape( ( px, py ) =>
		{
			var p = new Vector2( px, py ) - a;
			var t = Math.Clamp( Vector2.Dot( p, ab ) / lengthSq, 0f, 1f );
			return (p - ab * t).Length - width * 0.5f;
		}, MathF.Min( a.x, b.x ) - width, MathF.Min( a.y, b.y ) - width, MathF.Max( a.x, b.x ) + width, MathF.Max( a.y, b.y ) + width, color, alpha, height );
	}

	/// <summary>Tileable value noise in 0..1 at a lattice period that divides the texture size.</summary>
	float Noise( float x, float y, int period, int seed )
	{
		var cell = (float)Size / period;
		var gx = x / cell; var gy = y / cell;
		var x0 = (int)MathF.Floor( gx ); var y0 = (int)MathF.Floor( gy );
		var tx = Smooth( gx - x0 ); var ty = Smooth( gy - y0 );

		float Lattice( int lx, int ly )
		{
			lx = ((lx % period) + period) % period; ly = ((ly % period) + period) % period;
			var h = (uint)(lx * 374761393 + ly * 668265263 + seed * 2246822519u);
			h = (h ^ (h >> 13)) * 1274126177u;
			return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
		}

		var a = MathX.Lerp( Lattice( x0, y0 ), Lattice( x0 + 1, y0 ), tx );
		var b = MathX.Lerp( Lattice( x0, y0 + 1 ), Lattice( x0 + 1, y0 + 1 ), tx );
		return MathX.Lerp( a, b, ty );
	}

	float Fbm( float x, float y, int period, int seed )
	{
		var sum = 0f; var amp = 0.5f;
		for ( var o = 0; o < 4; o++ )
		{
			sum += Noise( x, y, period << o, seed + o * 17 ) * amp;
			amp *= 0.5f;
		}
		return sum / 0.9375f;
	}

	public void Mottle( float amount, int period, int seed )
	{
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
		{
			var i = y * Size + x;
			_color[i] *= 1f + (Fbm( x, y, period, seed ) - 0.5f) * 2f * amount;
		}
	}

	public void Grain( float amount, int seed )
	{
		var random = new System.Random( seed );
		for ( var i = 0; i < _color.Length; i++ )
			_color[i] *= 1f + ((float)random.NextDouble() - 0.5f) * 2f * amount;
	}

	public void HeightNoise( float amount, int period, int seed )
	{
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
			_height[y * Size + x] += (Fbm( x, y, period, seed ) - 0.5f) * amount;
	}

	/// <summary>
	/// The painterly two-tone: slopes facing the upper-left get lighter, the others darker, so the
	/// relief reads even where the scene light is flat. Done once, after all height is painted.
	/// </summary>
	public void ShadeFromHeight( float light, float shadow )
	{
		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
		{
			var slope = (_height[Index( x - 1, y )] - _height[Index( x + 1, y )]) + (_height[Index( x, y - 1 )] - _height[Index( x, y + 1 )]);
			var i = y * Size + x;
			_color[i] *= slope > 0f ? 1f + MathF.Min( slope, 1f ) * light : 1f + MathF.Max( slope, -1f ) * shadow;
		}
	}

	public Material ToMaterial( string name, string baseMaterial, float normalStrength )
	{
		var color = new byte[Size * Size * 4];
		var normal = new byte[Size * Size * 4];

		for ( var y = 0; y < Size; y++ )
		for ( var x = 0; x < Size; x++ )
		{
			var i = y * Size + x;
			var c = _color[i];
			color[i * 4 + 0] = (byte)(Math.Clamp( c.x, 0f, 1f ) * 255f);
			color[i * 4 + 1] = (byte)(Math.Clamp( c.y, 0f, 1f ) * 255f);
			color[i * 4 + 2] = (byte)(Math.Clamp( c.z, 0f, 1f ) * 255f);
			color[i * 4 + 3] = 255;

			var dx = (_height[Index( x + 1, y )] - _height[Index( x - 1, y )]) * normalStrength;
			var dy = (_height[Index( x, y + 1 )] - _height[Index( x, y - 1 )]) * normalStrength;
			EncodeNormal( new Vector3( -dx, -dy, 1f ).Normal, normal, i * 4 );
		}

		var colorTexture = Texture.Create( Size, Size, ImageFormat.RGBA8888 ).WithName( name + "_color" ).WithData( color ).WithMips().Finish();
		var normalTexture = Texture.Create( Size, Size, ImageFormat.RGBA8888 ).WithName( name + "_normal" ).WithData( normal ).WithMips().Finish();

		var material = Material.Load( baseMaterial ).CreateCopy( name );
		material.Set( "Color", colorTexture );
		material.Set( "Normal", normalTexture ); // name per spike finding 3
		material.Set( "g_flModelTintAmount", 1f );
		return material;
	}

	static void EncodeNormal( Vector3 n, byte[] target, int offset )
	{
		target[offset + 0] = (byte)((n.x * 0.5f + 0.5f) * 255f);
		target[offset + 1] = (byte)((n.y * 0.5f + 0.5f) * 255f);
		target[offset + 2] = (byte)((n.z * 0.5f + 0.5f) * 255f);
		target[offset + 3] = 255;
	}
}
```

- [ ] **Step 2: Compile** — `compile_status`: 0 errors. (If `WithMips()` without an argument does not compile, use `WithMips( null )` — the signature is `WithMips(int?)`.)

- [ ] **Step 3: Commit** — `git add Code/Art/Painter.cs` · `Painter: code-painted surfaces with height-derived normals`.

---

### Task 3: MeshKit (procedural geometry)

**Files:**
- Create: `Code/Art/MeshKit.cs`

**Interfaces — Consumes:** spike findings 1, 2, 5. **Produces:**
- `public readonly struct Skin` — `Skin( Material material, float repeat )` (tile: world units per texture repeat) and `Skin( Material material, Rect region )` (fit each face into an atlas region). Fields `Material`, `Repeat`, `Region`.
- `public sealed class MeshKit` with:
  - `void Quad( Skin skin, Vector3 a, Vector3 b, Vector3 c, Vector3 d )` — a→b→c→d anticlockwise seen from the front
  - `void Tri( Skin skin, Vector3 a, Vector3 b, Vector3 c )`
  - `void Box( Skin skin, Vector3 center, Vector3 size, Rotation rotation )`
  - `void BevelBox( Skin skin, Vector3 center, Vector3 size, float bevel, Rotation rotation )`
  - `void Lathe( Skin skin, Vector3 baseCenter, Vector2[] profile, int sides, Rotation rotation, bool capBottom = true, bool capTop = true )` — profile = (radius, height) pairs, bottom to top
  - `void Sphere( Skin skin, Vector3 center, float radius, int rings, int sides )`
  - `bool IsEmpty`, `Model Build( string name )`
- Convenience overloads without `rotation` use `Rotation.Identity`.

- [ ] **Step 1: Write `MeshKit.cs`** (`FlipWinding` set from spike finding 1)

```csharp
using System;
using System.Collections.Generic;

/// <summary>How a primitive's faces land on a material: tiled at a world scale, or fitted into an atlas cell.</summary>
public readonly struct Skin
{
	public readonly Material Material;

	/// <summary>World units per texture repeat. Zero means "fit": each face spans <see cref="Region"/> once.</summary>
	public readonly float Repeat;

	public readonly Rect Region;

	public Skin( Material material, float repeat )
	{
		Material = material;
		Repeat = repeat;
		Region = new Rect( 0f, 0f, 1f, 1f );
	}

	public Skin( Material material, Rect region )
	{
		Material = material;
		Repeat = 0f;
		Region = region;
	}
}

/// <summary>
/// Builds procedural geometry into one batch per material, then into a single Model — a whole room's
/// walls and props become a handful of draw calls. Flat-shaded on purpose: faceted props suit the
/// flat-colour look and keep the normals honest on bevels.
/// </summary>
public sealed class MeshKit
{
	/// <summary>Set by the Task 0 spike: true if the engine treats clockwise as front-facing.</summary>
	const bool FlipWinding = false;

	sealed class Batch
	{
		public readonly List<Vertex> Vertices = new();
		public readonly List<int> Indices = new();
	}

	readonly Dictionary<Material, Batch> _batches = new();
	BBox _bounds;
	bool _hasBounds;

	public bool IsEmpty => _batches.Count == 0;

	Batch For( Material material )
	{
		if ( !_batches.TryGetValue( material, out var batch ) )
			_batches[material] = batch = new Batch();
		return batch;
	}

	void Grow( Vector3 p )
	{
		_bounds = _hasBounds ? _bounds.AddPoint( p ) : new BBox( p, p );
		_hasBounds = true;
	}

	public void Quad( Skin skin, Vector3 a, Vector3 b, Vector3 c, Vector3 d )
	{
		var uAxis = (b - a).Normal;
		var normal = Vector3.Cross( b - a, d - a ).Normal;
		var vAxis = Vector3.Cross( normal, uAxis ); // up the face

		Vector4 Uv( Vector3 p, float fu, float fv )
		{
			if ( skin.Repeat > 0f )
				return new Vector4( Vector3.Dot( p, uAxis ) / skin.Repeat, -Vector3.Dot( p, vAxis ) / skin.Repeat, 0f, 0f );

			var r = skin.Region;
			return new Vector4( r.Left + fu * r.Width, r.Top + (1f - fv) * r.Height, 0f, 0f );
		}

		var batch = For( skin.Material );
		var start = batch.Vertices.Count;
		batch.Vertices.Add( new Vertex( a, normal, uAxis, Uv( a, 0f, 0f ) ) );
		batch.Vertices.Add( new Vertex( b, normal, uAxis, Uv( b, 1f, 0f ) ) );
		batch.Vertices.Add( new Vertex( c, normal, uAxis, Uv( c, 1f, 1f ) ) );
		batch.Vertices.Add( new Vertex( d, normal, uAxis, Uv( d, 0f, 1f ) ) );
		AddTriangle( batch, start, start + 1, start + 2 );
		AddTriangle( batch, start, start + 2, start + 3 );

		Grow( a ); Grow( b ); Grow( c ); Grow( d );
	}

	public void Tri( Skin skin, Vector3 a, Vector3 b, Vector3 c )
	{
		var uAxis = (b - a).Normal;
		var normal = Vector3.Cross( b - a, c - a ).Normal;
		var vAxis = Vector3.Cross( normal, uAxis );

		Vector4 Uv( Vector3 p, float fu, float fv )
		{
			if ( skin.Repeat > 0f )
				return new Vector4( Vector3.Dot( p, uAxis ) / skin.Repeat, -Vector3.Dot( p, vAxis ) / skin.Repeat, 0f, 0f );
			var r = skin.Region;
			return new Vector4( r.Left + fu * r.Width, r.Top + (1f - fv) * r.Height, 0f, 0f );
		}

		var batch = For( skin.Material );
		var start = batch.Vertices.Count;
		batch.Vertices.Add( new Vertex( a, normal, uAxis, Uv( a, 0f, 0f ) ) );
		batch.Vertices.Add( new Vertex( b, normal, uAxis, Uv( b, 1f, 0f ) ) );
		batch.Vertices.Add( new Vertex( c, normal, uAxis, Uv( c, 0.5f, 1f ) ) );
		AddTriangle( batch, start, start + 1, start + 2 );

		Grow( a ); Grow( b ); Grow( c );
	}

	static void AddTriangle( Batch batch, int a, int b, int c )
	{
		batch.Indices.Add( a );
		batch.Indices.Add( FlipWinding ? c : b );
		batch.Indices.Add( FlipWinding ? b : c );
	}

	/// <summary>A quad whose winding is fixed to face <paramref name="outward"/>, whatever order the corners came in.</summary>
	void QuadFacing( Skin skin, Vector3 outward, Vector3 a, Vector3 b, Vector3 c, Vector3 d )
	{
		if ( Vector3.Dot( Vector3.Cross( b - a, d - a ), outward ) >= 0f ) Quad( skin, a, b, c, d );
		else Quad( skin, d, c, b, a );
	}

	void TriFacing( Skin skin, Vector3 outward, Vector3 a, Vector3 b, Vector3 c )
	{
		if ( Vector3.Dot( Vector3.Cross( b - a, c - a ), outward ) >= 0f ) Tri( skin, a, b, c );
		else Tri( skin, a, c, b );
	}

	public void Box( Skin skin, Vector3 center, Vector3 size ) => Box( skin, center, size, Rotation.Identity );

	public void Box( Skin skin, Vector3 center, Vector3 size, Rotation rotation ) => BevelBox( skin, center, size, 0f, rotation );

	public void BevelBox( Skin skin, Vector3 center, Vector3 size, float bevel ) => BevelBox( skin, center, size, bevel, Rotation.Identity );

	/// <summary>
	/// A box with every edge chamfered by <paramref name="bevel"/>: six inset faces, twelve edge strips,
	/// eight corner triangles. The chamfers catch light, which is what makes a generated block read
	/// as an object rather than a grey cut-out. Zero bevel gives a plain six-face box.
	/// </summary>
	public void BevelBox( Skin skin, Vector3 center, Vector3 size, float bevel, Rotation rotation )
	{
		var h = size * 0.5f;
		bevel = MathF.Min( bevel, MathF.Min( h.x, MathF.Min( h.y, h.z ) ) * 0.9f );
		var i = new Vector3( h.x - bevel, h.y - bevel, h.z - bevel );

		Vector3 P( float x, float y, float z ) => center + rotation * new Vector3( x, y, z );
		Vector3 Out( float x, float y, float z ) => rotation * new Vector3( x, y, z );

		for ( var s = -1; s <= 1; s += 2 )
		{
			QuadFacing( skin, Out( s, 0, 0 ), P( s * h.x, -i.y, -i.z ), P( s * h.x, i.y, -i.z ), P( s * h.x, i.y, i.z ), P( s * h.x, -i.y, i.z ) );
			QuadFacing( skin, Out( 0, s, 0 ), P( -i.x, s * h.y, -i.z ), P( i.x, s * h.y, -i.z ), P( i.x, s * h.y, i.z ), P( -i.x, s * h.y, i.z ) );
			QuadFacing( skin, Out( 0, 0, s ), P( -i.x, -i.y, s * h.z ), P( i.x, -i.y, s * h.z ), P( i.x, i.y, s * h.z ), P( -i.x, i.y, s * h.z ) );
		}

		if ( bevel <= 0f ) return;

		for ( var sa = -1; sa <= 1; sa += 2 )
		for ( var sb = -1; sb <= 1; sb += 2 )
		{
			// Edges along Z (between the X and Y faces), along Y (X and Z), along X (Y and Z).
			QuadFacing( skin, Out( sa, sb, 0 ),
				P( sa * h.x, sb * i.y, -i.z ), P( sa * h.x, sb * i.y, i.z ), P( sa * i.x, sb * h.y, i.z ), P( sa * i.x, sb * h.y, -i.z ) );
			QuadFacing( skin, Out( sa, 0, sb ),
				P( sa * h.x, -i.y, sb * i.z ), P( sa * h.x, i.y, sb * i.z ), P( sa * i.x, i.y, sb * h.z ), P( sa * i.x, -i.y, sb * h.z ) );
			QuadFacing( skin, Out( 0, sa, sb ),
				P( -i.x, sa * h.y, sb * i.z ), P( i.x, sa * h.y, sb * i.z ), P( i.x, sa * i.y, sb * h.z ), P( -i.x, sa * i.y, sb * h.z ) );

			for ( var sc = -1; sc <= 1; sc += 2 )
				TriFacing( skin, Out( sa, sb, sc ), P( sa * h.x, sb * i.y, sc * i.z ), P( sa * i.x, sb * h.y, sc * i.z ), P( sa * i.x, sb * i.y, sc * h.z ) );
		}
	}

	public void Lathe( Skin skin, Vector3 baseCenter, Vector2[] profile, int sides ) => Lathe( skin, baseCenter, profile, sides, Rotation.Identity );

	/// <summary>
	/// Spins a (radius, height) profile around the local Z axis — pots, lamps, pillars, bottles, cones.
	/// Faceted: each segment is its own quad, so a 10-sided pot reads as a crafted object.
	/// </summary>
	public void Lathe( Skin skin, Vector3 baseCenter, Vector2[] profile, int sides, Rotation rotation, bool capBottom = true, bool capTop = true )
	{
		Vector3 Ring( int step, Vector2 rz )
		{
			var angle = step * MathF.Tau / sides;
			return baseCenter + rotation * new Vector3( MathF.Cos( angle ) * rz.x, MathF.Sin( angle ) * rz.x, rz.y );
		}

		for ( var p = 0; p < profile.Length - 1; p++ )
		for ( var s = 0; s < sides; s++ )
		{
			var a = Ring( s, profile[p] );
			var b = Ring( s + 1, profile[p] );
			var c = Ring( s + 1, profile[p + 1] );
			var d = Ring( s, profile[p + 1] );
			var mid = (a + b + c + d) * 0.25f;
			var axisPoint = baseCenter + rotation * new Vector3( 0f, 0f, (profile[p].y + profile[p + 1].y) * 0.5f );
			var outward = mid - axisPoint;

			var bottomApex = profile[p].x <= 0.0001f;
			var topApex = profile[p + 1].x <= 0.0001f;

			if ( bottomApex && topApex ) continue;
			if ( topApex ) TriFacing( skin, outward, a, b, c );        // c == d: cone tip above
			else if ( bottomApex ) TriFacing( skin, outward, a, c, d ); // a == b: cone tip below
			else QuadFacing( skin, outward, a, b, c, d );
		}

		var up = rotation * Vector3.Up;
		if ( capBottom && profile[0].x > 0.0001f )
		{
			var centre = baseCenter + rotation * new Vector3( 0f, 0f, profile[0].y );
			for ( var s = 0; s < sides; s++ )
				TriFacing( skin, -up, centre, Ring( s, profile[0] ), Ring( s + 1, profile[0] ) );
		}

		var last = profile[^1];
		if ( capTop && last.x > 0.0001f )
		{
			var centre = baseCenter + rotation * new Vector3( 0f, 0f, last.y );
			for ( var s = 0; s < sides; s++ )
				TriFacing( skin, up, centre, Ring( s, last ), Ring( s + 1, last ) );
		}
	}

	public void Sphere( Skin skin, Vector3 center, float radius, int rings, int sides )
	{
		var profile = new Vector2[rings + 1];
		for ( var r = 0; r <= rings; r++ )
		{
			var a = MathF.PI * r / rings - MathF.PI * 0.5f;
			profile[r] = new Vector2( MathF.Cos( a ) * radius, MathF.Sin( a ) * radius + radius );
		}
		Lathe( skin, center - Vector3.Up * radius, profile, sides, Rotation.Identity, false, false );
	}

	public Model Build( string name )
	{
		var builder = Model.Builder.WithName( name );

		foreach ( var (material, batch) in _batches )
		{
			var mesh = new Mesh( material );
			mesh.CreateVertexBuffer( batch.Vertices.Count, Vertex.Layout, batch.Vertices );
			mesh.CreateIndexBuffer( batch.Indices.Count, batch.Indices );
			mesh.Bounds = _bounds;
			builder.AddMesh( mesh );
		}

		return builder.Create();
	}
}
```

- [ ] **Step 2: Compile** — 0 errors. The `Vertex` constructor takes `Vector3 tangent`; if tangent sign matters for normal maps (spike), switch to setting `vertex.Tangent = new Vector4( uAxis, sign )` after construction.

- [ ] **Step 3: Visual check** — temporary 5 lines in `ArenaGenerator.Generate` building one `BevelBox`, one `Lathe` pot and one `Sphere` with a `Painter` brick material at (0, 0, 120); screenshot; check no missing faces, bevels lit, texture scale right. Remove the 5 lines.

- [ ] **Step 4: Commit** — `git add Code/Art/MeshKit.cs` · `MeshKit: procedural meshes batched per material`.

---

### Task 3b: Painter realism (roughness channel, cavity, wear, detail)

Added 2026-10-11 after the user asked for a "very realistic" look and chose **stylised realism**
(spec, Textures section). Tasks 0–3 stay valid; this extends `Painter` before any surface is painted.

**Files:**
- Modify: `Code/Art/Painter.cs`

**Interfaces — Consumes:** Task 2 `Painter`. **Produces (additions; existing signatures stay source-compatible):**
- `Painter( int size, Color fill, float roughness = 0.75f )` — per-pixel roughness channel, filled with `roughness`.
- `Shape(…)`, `Rect(…)`, `Circle(…)`, `Line(…)` gain an optional trailing `float roughness = float.NaN` — when a number, the shape writes it into the roughness channel with the same coverage as colour.
- `void RoughnessNoise( float amount, int period, int seed )` — fbm variation of roughness (smudges, polish, wear).
- `Mottle` / `HeightNoise` gain an optional `int octaves = 4` (max 6) for finer detail at 1024².
- `void Cavity( float amount, int radius )` — ambient-occlusion bake: blur the height field (separable box blur, wrapping), darken colour where height is below its neighbourhood (`colour *= 1 − amount · clamp( (blur − h) · k )`), and raise roughness slightly there (dust collects in crevices).
- `void EdgeWear( float amount, int radius, int seed )` — where height is above its neighbourhood (convex edges), lighten colour toward a worn tone and lower roughness slightly, masked by fbm noise so wear is patchy.
- `ToMaterial( string name, string baseMaterial, float normalStrength )` — writes per-pixel roughness into the normal texture's B channel. The old `roughness` parameter of `ToMaterial` is removed (the constructor carries the fill value); update any caller (there are none outside Painter yet).

- [ ] **Step 1: Verify the roughness channel in-engine (temporary, uncommitted).** Two 128² test materials identical except B = 20 vs 240, on two tiles of floor 0 next to each other, a `PointLight` close and low so a highlight falls across both. Screenshot. Expected: the B=20 tile shows a tight bright highlight, the B=240 tile a broad dim one. If they look identical, B does not drive roughness: try `material.Set( "g_flRoughnessScaleFactor", … )` / inspect `core/materials/dev/dev_nonmetal_rough30.vmat_c` strings for a roughness parameter, and record what works. Record the finding in the plan under this task and in memory `sbox-procedural-mesh`.
- [ ] **Step 2:** Implement the additions above. Blur must wrap so textures stay seamless. Keep everything deterministic (seeded).
- [ ] **Step 3: Performance.** Time a 1024² surface with 3 fbm passes (6 octaves) + Cavity + EdgeWear + `ToMaterial` (use `System.Diagnostics.Stopwatch` if allowed by the sandbox, else `RealTime.Now` deltas); log it once. Target ≤ 120 ms per 1024² surface. If over, optimise the hot loops (precompute lattice per row, avoid closures in inner loops) before committing.
- [ ] **Step 4: Visual check (temporary, uncommitted).** A 1024² flagstone test (3 irregular stones from rounded rects with height + bevel, HeightNoise for chisel texture, Cavity 0.5 radius 6, EdgeWear 0.4, RoughnessNoise 0.2, Mottle with 6 octaves) on 4 floor-0 tiles; screenshots straight down and at a grazing angle under the moving PointLight. It must read as stone: dark joints, lighter worn edges, varied sheen.
- [ ] **Step 5:** Remove temp code (`git checkout -- Code/ArenaGenerator.cs` afterwards), `compile_status` clean, commit `Painter: roughness channel, cavity and edge wear for realistic surfaces`.

**Roughness finding:** (filled in by Step 1)

---

### Task 4: Theme framework + themed tiles (all eight floor surfaces)

**Files:**
- Create: `Code/Arena/ArenaTheme.cs`, `Code/Arena/Themes/GreenhouseTheme.cs`, `WinterTheme.cs`, `BeachTheme.cs`, `OfficeTheme.cs`, `LibraryTheme.cs`, `WorkshopTheme.cs`, `DungeonTheme.cs`, `ForgeTheme.cs`
- Modify: `Code/ArenaGenerator.cs` (tile build, `TileMaterial`, `TileColor`, `RestoreTile`, remove `IsEdge` banding and `_hazardMaterial`)

**Interfaces — Consumes:** `Palette`, `Painter`, `MeshKit`, `Skin`. **Produces:**

```csharp
public abstract class ArenaTheme
{
	public abstract string Name { get; }
	public abstract int Floor { get; }

	/// <summary>Walkable surface, 2x2 atlas of four variants (each cell = one tile top).</summary>
	public Material FloorSurface { get; private set; }

	/// <summary>What you see looking up in this room: the underside of the floor above.</summary>
	public Material Ceiling { get; private set; }

	/// <summary>Main wall surface, tiled at <see cref="WallRepeat"/> world units.</summary>
	public Material Wall { get; private set; }

	/// <summary>Trim/props atlas: skirting, cornice, frames and small props, 4x4 cells.</summary>
	public Material Trim { get; private set; }

	public virtual float WallRepeat => 208f;

	/// <summary>Interior light colour (HDR allowed) used by TowerBuilder and props.</summary>
	public abstract Color LightColor { get; }

	public void Paint()
	{
		FloorSurface = PaintFloor();
		Ceiling = PaintCeiling();
		Wall = PaintWall();
		Trim = PaintTrim();
	}

	protected abstract Material PaintFloor();
	protected abstract Material PaintCeiling();
	protected abstract Material PaintWall();
	protected abstract Material PaintTrim();

	/// <summary>Dress the room: wall face layer, alcove props, ceiling props, lights. Task 6+.</summary>
	public abstract void Dress( FloorContext room );

	public static Rect Cell( int column, int row, int cells ) => new( column / (float)cells, row / (float)cells, 1f / cells, 1f / cells );

	public static ArenaTheme Create( int floor ) => floor switch
	{
		0 => new GreenhouseTheme(), 1 => new WinterTheme(), 2 => new BeachTheme(), 3 => new OfficeTheme(),
		4 => new LibraryTheme(), 5 => new WorkshopTheme(), 6 => new DungeonTheme(), _ => new ForgeTheme(),
	};
}
```

`FloorContext` is declared here with only what Task 4 needs (`int Floor`, `float TileTopZ`) and grows in Task 6.

Tile model per floor and variant: `MeshKit.BevelBox` 56×56×12, bevel 2, but faces split by material: top + sides → `new Skin( theme.FloorSurface, ArenaTheme.Cell( v % 2, v / 2, 2 ) )`; underside → `new Skin( below.Ceiling, Arena.TileSize )` where `below = themes[floor + 1]` (floor 7 uses its own Ceiling). Implement as a dedicated `BuildTileModel( ArenaTheme self, ArenaTheme below, int variant )` in `ArenaGenerator` using `MeshKit.Quad` for the bottom face and `BevelBox` with the bottom face left out — add `bool skipBottom` parameter to `BevelBox` (default false) in this task.

Variant choice: `variant = Hash( floor, x, y ) & 3` with `Hash = (x * 73856093) ^ (y * 19349663) ^ (floor * 83492791)`, unsigned.

Tile GameObject: `LocalScale = Vector3.One`, `BoxCollider.Scale = new Vector3( Arena.TileSize, Arena.TileSize, Arena.TileThickness )`, `ModelRenderer.Model = _tileModels[floor, variant]`, no `MaterialOverride` at rest, `Tint = TileColor(floor,x,y)` = `Color.White.Darken( (x + y) % 2 == 0 ? 0f : 0.04f )`.

`SetTileWarning` keeps `MaterialOverride = _glowMaterial` (it now lights the underside too — intended, see spec). `RestoreTile` sets `MaterialOverride = null`, tint and position.

**Realism rules for every surface in this and later tasks (stylised realism, spec Textures section):** paint real structure in height (joints, grain, chips, weave, ripples) with bevels; then `Cavity` (0.35–0.6), `EdgeWear` where the material wears (stone, wood, metal, terracotta — not snow), `RoughnessNoise`, `Mottle` with 5–6 octaves for large-scale colour variation, `Grain` ≤ 0.02. Per-material roughness: snow 0.55 with sparkle flecks at 0.15, ice 0.2, terracotta glaze 0.35 / body 0.8, sand 0.9, carpet 0.95, polished oak 0.35, steel plate on `Painter.Metal` 0.45, granite 0.7 with wet-ish joints 0.5, basalt 0.8. Colours are natural material colours chosen near the floor's identity hue (spec Palette section); the hexes below are starting points, tuned on screenshots. Log total texture generation time once at startup (spec budget ≤ 4 s).

**Floor surface recipes** (1024², 2×2 atlas → each cell 512 px = one 56-unit tile; every cell has a real bevelled block edge in height + a darker joint so tiles read as separate blocks; colours from the theme's own palette):

| Floor | Cell art | Base / light / shade / accent |
|---|---|---|
| Greenhouse | Terracotta tile, two hand-made glaze highlights, chipped corner on variants 1,3 | `#D98E6A` / `#E9A988` / `#B8704F` / sage `#A9CF95` moss in one corner of variant 2 |
| Winter | Packed snow: soft drifts (HeightNoise 0.4, period 4), ice showing through in 1–2 blue patches, sparkle dots | `#EEF3F8` / `#FFFFFF` / `#C8D6E6` / ice `#A7CBE8` |
| Beach | Sand ripples (sine bands in height, 3 per cell, warped by noise), shell or starfish on variants 1,2 | `#F0D9A8` / `#F8E8C4` / `#D9BD86` / coral `#E9A39A` |
| Office | Carpet tile, two-tone weave (1-px stripes alternating direction per cell — the classic quarter-turn carpet) | `#8C97B8` / `#A3ADCB` / `#76809F` / periwinkle `#9FB0DA` |
| Library | Herringbone parquet: 8 planks per cell at ±45°, each plank ±6% tone, grain lines | `#B98A62` / `#CFA27A` / `#9C7050` / rose `#C99BB0` inlay dot on variant 3 |
| Workshop | Checker plate: diamond tread bosses on a 6×6 lattice, riveted corners, oil stain on variant 2 | `#9BA1A8` / `#C3C8CE` / `#767C84` / brass `#C9A26B` rivets |
| Dungeon | Flagstones: 2–3 irregular stones per cell (Voronoi from 3 seeded points), moss in joints | `#8D8798` / `#A49FAE` / `#6F6A7A` / moss `#7FA06A` |
| Forge | Basalt: hex-ish columns tops, dark, thin ember cracks (`Palette.Lava` at 60%) — the only floor allowed a danger colour, because it sits on the lava | `#4A4250` / `#5E5566` / `#352F3A` / `Palette.Lava` |

Ceilings (seen from below; 512², same realism rules): Greenhouse — none (glass roof; tiles of floor 0 have floor 1's ceiling under them). Winter — pine boards `#C9A27A`. Beach — sailcloth stripes `#F4ECDC`/`#E9A39A`. Office — 2×2 acoustic ceiling tiles `#E8E4DA` with pinholes. Library — dark walnut coffers `#6E4F3E`. Workshop — galvanised sheet `#8E959C`. Dungeon — rough stone `#5E5866`. Forge — rock `#3B3440` with ember glints. (Floor 7's tile undersides face the lava shaft and use the Forge ceiling.)

Walls and trims for each theme are painted in this task as simple, correct first versions (base colour + one structural pattern each: glazing grid, log courses, planks, partition panels, bookcase grid, corrugation, ashlar, rock) so Task 6 has something to build; Tasks 7–10 refine them with the props.

- [ ] **Step 1:** Write `ArenaTheme.cs` (class above + `FloorContext` with `Floor`, `TileTopZ`).
- [ ] **Step 2:** Write the eight theme files: `Name`, `Floor`, `LightColor`, four `Paint*` methods per the tables, `Dress` empty-bodied with a one-line comment `// Room dressing arrives in Task 6.` (Task 6 replaces it; an empty override is a real no-op, not a placeholder in the plan sense).
- [ ] **Step 3:** Add `skipBottom` to `MeshKit.BevelBox`.
- [ ] **Step 4:** Modify `ArenaGenerator.Generate`: create `_themes[8]` (call `Paint()` on each), `_tileModels[8,4]`, build tiles as above; delete `_floorMaterials`, `_hazardMaterial`, `IsEdge`, `TileMaterial`; rewrite `TileColor`, `RestoreTile`.
- [ ] **Step 5: Compile** — 0 errors. Check `TileWarning.cs` and `AbilityFx.cs` still compile (they only call `SetTileWarning` / `RestoreTile` / `TileColor`).
- [ ] **Step 6: Screenshots** — play; on each floor: standing view, looking straight down at 4 tiles, and from floor n looking up (hole in floor n−1 made by `TileDecay.ForceCollapse` from the console if available, else walk). Check: variants visible, seams read, colours inside band, no texture swimming, warning glow visible from below.
- [ ] **Step 7: Reset check** — trigger a round reset (end round or `ResetAll` via console) after several warnings; tiles return to themed material (no glow left).
- [ ] **Step 8: Commit** — `Themed tile surfaces for all eight floors`.
- [ ] **Checkpoint — user tests:** floors look different, tiles readable while running, warnings visible from above and below.

---

### Task 5: (merged into Task 4 — tiles and surfaces ship together)

---

### Task 6: TowerBuilder — walls, collision, ceilings, roof, facade, lights, lava shaft

**Files:**
- Create: `Code/Arena/TowerBuilder.cs`
- Modify: `Code/Arena/ArenaTheme.cs` (grow `FloorContext`), `Code/ArenaGenerator.cs` (call `TowerBuilder`, delete `CreateFrame`, `AddBox`, pillar constants, `EdgeColor`, `FrameColor`; `CreateLava` sized to `span`; `CreateSigns` → wall plaques; fog/sky retune), `Code/ui/ArenaSign.razor` + `.scss` (plaque, no `PatternUrl`)

**Interfaces — Produces** (`FloorContext` final shape):

```csharp
public sealed class FloorContext
{
	public int Floor { get; init; }
	public float TileTopZ { get; init; }          // local z of this floor's walkable surface
	public float CeilingZ { get; init; }          // local z of the underside above (TileTopZ + 208), roof base for floor 0
	public float BottomZ { get; init; }           // local z where this floor's wall segment starts (tile underside, or lava for floor 7)
	public MeshKit Kit { get; init; }             // this floor's batch; everything the theme adds lands in one Model
	public ArenaTheme Theme { get; init; }
	public const float HalfSpan = Arena.GridSize * Arena.TileSize * 0.5f; // 840
	public const float WallThickness = 64f;
	public const float ClearHeight = Arena.FloorGap - Arena.TileThickness; // 208

	/// <summary>Wall-space → room-local: side 0..3 (+X,+Y,-X,-Y), u along the wall from its left end
	/// (0..1680, seen from inside), v up from the tile top, depth into the wall (0 = grid edge, + = into wall).</summary>
	public Vector3 OnWall( int side, float u, float v, float depth );

	/// <summary>Rotation whose Forward points out of the wall into the room, Up = world up.</summary>
	public Rotation WallFacing( int side );

	public void AddLight( Vector3 localPosition, Color color, float radius ); // PointLight, Shadows=false, ≤ 6 per floor (asserts in editor via Log.Warning)
}
```

TowerBuilder per floor:
1. Structural shell in the theme's `Wall` skin: back plane at depth 40 from `BottomZ` to `CeilingZ`, wall top cap, and the band between `TileTopZ − 6` and `BottomZ` (floor slab edge) in `Trim`.
2. Outer facade (shared `Paper` stone material painted once in TowerBuilder: ashlar courses, 512²): outer face at depth 64, a window band per floor in `FloorHue(floor)` frames with dark `Ink` glass, a cornice ring between floors.
3. Colliders: one child GameObject per floor, four `BoxCollider`s, each `Center` at depth +32 (so the inner face sits at exactly ±840), `Scale` = (1680 + 128, 64, `CeilingZ − BottomZ`) rotated per side, `Static = true`.
4. `theme.Dress( room )` — Task 6 implements, for each theme, only the structural front layer (pilasters every 210 units, skirting, cornice) and its lights; props come in Tasks 7–10.
5. Floor 0: greenhouse roof lattice (glazing bars as BevelBox 4×4, ridge at +40); panes per spike finding 4 (translucent copy with `RenderType = Off` for shadows) or open.
6. Lava shaft: floor 7's `BottomZ` = lava top; lava slab `span × span`.
7. `kit.Build( $"tower_floor{floor}" )` → one `ModelRenderer` per floor on GameObject `Floor {n} Shell`, `NotSaved`, `NetworkMode.Never`.

- [ ] **Step 1:** Grow `FloorContext` and write `OnWall`, `WallFacing`, `AddLight`.
- [ ] **Step 2:** Write `TowerBuilder.cs` with steps 1–7 above; facade material painted here.
- [ ] **Step 3:** Structural `Dress` for all eight themes (pilasters/skirting/cornice in their Trim, 4–6 lights each at `CeilingZ − 30` inside cornice coves).
- [ ] **Step 4:** `ArenaGenerator`: remove frame, posts, edge ring, glowing caps; signs become a `WorldPanel` plaque centred on each of the four walls per floor at v=150, depth −1, facing into the room; `ArenaSign` drops `PatternUrl` and paints a `Paper` plaque with the floor number in `Ink` and a `FloorHue` rim. Trophy sign stays above the roof.
- [ ] **Step 5:** Sky colour → `#BFD8E8` (a softer sky inside the palette), fog start 2500 / end 9000, colour `#D9CFE3`.
- [ ] **Step 6: Compile** — 0 errors.
- [ ] **Step 7: Edge drop check** — on floors 0 and 7, collapse the edge tile column next to a wall (stand on it in Playing); the player must fall to the floor below without catching on the wall. Compare collider bounds in `get_game_object` against ±840.
- [ ] **Step 8: Edge camera** — standing at a wall, rotate 360° and look up: screenshot ×3 per floor on floors 0, 3, 7. The boom must pull in; no outside, no void.
- [ ] **Step 9: Exterior** — `MenuCamera` orbit screenshot: one calm tower, eight window bands.
- [ ] **Step 10: Performance** — frame time in play from floor 3 compared with before this task (record both in the commit message).
- [ ] **Step 11: Commit** — `Tower shell: enclosed floors with walls, roof and facade`.
- [ ] **Checkpoint — user tests:** cannot fall off the side; holes still drop cleanly next to walls; camera behaves at walls; abilities Launch/Hook against walls.

---

### Tasks 7–10: Room dressing (props + refined wall/trim art), two floors per task

Each task has the same shape. **Files:** the two theme files; nothing else unless a `MeshKit` primitive is missing (then add it to `MeshKit.cs` with the same `QuadFacing`/`TriFacing` approach and say so in the commit).

Placement rules for every prop (from the spec): inside the wall's 64-unit thickness below v=140; ceiling props above v=140 overhang ≤ 24; props are batched into the floor's `Kit` (no per-prop GameObjects, except `PointLight`s and emissive props that need their own renderer for a different material — those also go through `Kit`, which batches by material automatically); four walls are dressed by looping `side` 0..3 with a per-side seed so they differ.

**Task 7 — Floors 1 Greenhouse, 2 Winter**
- Greenhouse: wall = glazing (sage bars over pale sky-tinted glass panels, not translucent); planting beds along the skirting (BevelBox planters 40 deep, soil top, 6–9 lathe pots per wall with leaf clusters of flattened spheres in two greens); hanging ferns from roof bars (cone + sphere clusters); potting shelves with watering can (lathe body + box spout), seed trays; roof as built in Task 6, refined bar profile.
- Winter: wall = horizontal log courses (lathe-faced logs half-sunk into the wall, ends with ring texture in Trim), frosted windows every 420 units (white frame, `#DDEBF5` frosted pane with painted frost corners), icicles along the cornice (cones of varied length, `#EAF4FB`), snow drifts in window sills (flattened spheres), skis crossed on two walls, wreaths (torus approximated by 12 small spheres), a snowman relief in one alcove per wall (three spheres, coal dots, carrot cone in `Warn`? — no: carrot uses accent `#E9A06A`, danger colours are reserved), pine beam ceiling with bulb string (small emissive spheres, warm).

**Task 8 — Floors 3 Beach, 4 Office**
- Beach: wall = painted beach-hut planks in alternating pastel bands, striped awnings over alcoves (sloped quads, stripes in Trim), surfboards leaning in alcoves (flattened lathe), life ring (sphere ring), palm fronds at the cornice (fan of thin boxes), beach balls (sphere with 6-segment stripe material in Trim), sail ceiling (gently sagging quads between beams).
- Office: wall = partition panels (fabric texture) alternating with windows (white frames, blinds as 12 thin slats, pale sky beyond), whiteboards with marker scribbles painted in Trim, wall clocks (lathe disc + hands), filing cabinets in alcoves (BevelBox with drawer lines + handles), water cooler (lathe bottle `#BFD8E8`), potted plant per wall, grid ceiling rails along the cornice with light panels (emissive, cool white).

**Task 9 — Floors 5 Library, 6 Workshop**
- Library: wall = full-height bookcases: shelves every 34 units, books generated per shelf (width 4–9, height 22–30, tilt ±6° for the last few, colours from a 6-colour muted set inside the band, spine bands in Trim), rolling ladder on a rail on two walls, reading lamps (lathe brass + green shade, emissive bulb) every 420, framed maps above shelves on one wall, coffered ceiling cornice, chandeliers (lathe + small emissive spheres) hanging ≤ 24 inward.
- Workshop: wall = corrugated sheet (sine height in texture) between riveted steel posts, pipe runs along two heights with elbows (lathe segments), valves (wheels = spheres ring), pressure gauges (lathe disc, painted dial), gears on the wall (flattened lathe with teeth as boxes), hazard-stripe trim (Ink + PaperShade — not Warn), cage lamps (lathe + box cage) under girders.

**Task 10 — Floors 7 Dungeon, 8 Forge**
- Dungeon: wall = ashlar stone (irregular block sizes painted with deep mortar height), vaulted arches springing from pilasters (voussoirs as rotated BevelBoxes), torches every 280 units (box bracket + lathe holder + emissive flame cone in warm `#FFC27A` — flames are not danger), chains hanging in alcoves (rings of small boxes), barred windows (dark recess + bars), banners in `FloorHue(6)` with an emblem painted in Trim, a skull niche (sphere + jaw box) on one wall.
- Forge: wall = dark rock with lava seams (Lava colour in narrow painted seams, emissive copy of the wall material for seam strips), forge mouths (arched recess with emissive back), anvils (BevelBox stack), tool racks (thin boxes), bellows (lathe + wedge), stalactites hanging from the ceiling cornice (cones), the shaft below floor 7 rendered as raw rock to the lava.

Each of Tasks 7–10:
- [ ] **Step 1:** Refine the two themes' `PaintWall` / `PaintTrim` to the descriptions above.
- [ ] **Step 2:** Write their `Dress` props.
- [ ] **Step 3: Compile** — 0 errors.
- [ ] **Step 4: Screenshots** — per floor: centre view toward each wall (4), a corner, looking up. Compare with the references; fix anything that reads as noise, any colour outside the band, any prop poking into the play volume below v=140.
- [ ] **Step 5: Commit** — `Dress floors N and M: <themes>`.
- [ ] **Checkpoint — user tests:** both floors.

---

### Task 11: Menu orbit, cleanup, docs

**Files:**
- Modify: `Code/MenuCamera.cs` (orbit radius/height so the full tower and roof fit; slight downward look on the greenhouse)
- Delete: `Code/TileTextures.cs` (after moving `CreateLava` into `ForgeTheme` / `ArenaGenerator`), `Assets/textures/kenney/tile_floor*.png`, `tile_edge.png`, `frame_bricks.png` (+ `.meta`) if nothing references them (`grep -rn "textures/kenney" Code`)
- Modify: `CLAUDE.md` status section; memory: `sbox-procedural-mesh`, update `sbox-render-audio-findings` if lighting lessons were learned

- [ ] **Step 1:** Menu orbit tuning + screenshot.
- [ ] **Step 2:** Cleanup + `compile_status` 0 errors.
- [ ] **Step 3:** CLAUDE.md status paragraph: what the tower is, where themes live, the rules (props in wall thickness, danger colours reserved).
- [ ] **Step 4: Commit** — `Arena themes: menu framing, cleanup, status notes`.
- [ ] **Checkpoint — user tests:** full round top to bottom; then the UI pass gets its own design round (spec step 8).
