using System;

/// <summary>
/// Tile surfaces generated in code at startup — no texture files. CLAUDE.md keeps the project to
/// built-in assets and code-generated content; a texture computed from a fixed seed is the same
/// kind of thing as the arena itself, just in pixels.
///
/// Every texture is greyscale (or near it) on purpose: the floor's identity colour still comes
/// from ModelRenderer.Tint, so one block texture serves every floor.
///
/// The look is a 16x16 "pixel" block, upscaled with hard edges: a soft candy block in the spirit
/// of a toy set — smooth fill, rounded corners, a pillowy light rim top-left and a gentle shadow
/// bottom-right, so every tile reads as a separate block even on a single-colour pastel floor.
/// </summary>
public static class TileTextures
{
	/// <summary>Logical pixels per side — the block's art resolution.</summary>
	const int ArtSize = 16;

	/// <summary>Texture pixels per logical pixel. Upscaling in code keeps the pixels crisp.</summary>
	const int Upscale = 8;

	const int Size = ArtSize * Upscale;

	// Deliberately no static cache. s&box hotload keeps static fields alive across code reloads and
	// play sessions, so a cached material outlives the code that built it — every texture change
	// silently kept showing the first version. The caller builds these once per arena and holds them.

	/// <summary>The ordinary floor block, generated. Fallback if a Kenney texture fails to load.</summary>
	public static Material CreateBlock() => CreateMaterial( "tnt_block", BlockPixels( 1337 ) );

	/// <summary>
	/// The user's Kenney pattern-pack motifs, baked into greyscale tile textures with the block bevel
	/// (Assets/textures/kenney, made from kenney_pattern-pack.zip). One motif per floor, so each floor
	/// reads as its own place — dots, hearts, diamonds, hexagons, stars, checks, pebbles, octagons.
	/// Greyscale like the generated ones, so ModelRenderer.Tint still carries the floor colour.
	/// </summary>
	public static string FloorTexturePath( int floor ) => $"textures/kenney/tile_floor{floor % 8}.png";

	public const string EdgeTexturePath = "textures/kenney/tile_edge.png";
	public const string FrameTexturePath = "textures/kenney/frame_bricks.png";
	public const string LavaTexturePath = "textures/kenney/lava_blobs.png";

	/// <summary>
	/// A copy of <paramref name="baseMaterial"/> wearing a texture file from the project, or null if
	/// it didn't load — callers fall back to a generated texture, so a missing file degrades the look
	/// rather than painting the arena in the engine's error checkerboard.
	/// </summary>
	public static Material CreateFromFile( string name, string path, string baseMaterial = ArenaGenerator.SurfaceMaterialPath )
	{
		var texture = Texture.LoadFromFileSystem( path, FileSystem.Mounted, true );
		if ( texture is null || texture.IsError )
		{
			Log.Warning( $"TileTextures: couldn't load {path}, using the generated texture instead" );
			return null;
		}

		var material = Material.Load( baseMaterial ).CreateCopy( name );
		material.Set( "Color", texture );
		material.Set( "g_flModelTintAmount", 1f );
		return material;
	}

	/// <summary>Diagonal warning stripes for the outermost ring. Tint it a warning colour.</summary>
	public static Material CreateHazard() => CreateMaterial( "tnt_hazard", HazardPixels() );

	/// <summary>Riveted steel plate for the frame and posts.</summary>
	public static Material CreatePlate( string baseMaterial ) => CreateMaterial( "tnt_plate", PlatePixels(), baseMaterial );

	/// <summary>
	/// Cracked crust for the lava, on a copy of the emissive material: bright seams between dark
	/// plates, so the slab glows through its cracks instead of as one flat orange sheet.
	/// </summary>
	public static Material CreateLava( string baseMaterial )
	{
		// The Kenney blob pattern when it loads; the generated crust otherwise. Both tile seamlessly.
		var material = CreateFromFile( "tnt_lava", LavaTexturePath, baseMaterial )
			?? CreateMaterial( "tnt_lava", LavaPixels( 4242 ), baseMaterial );

		// The slab is one box ~1900 units across; repeat the 16-pixel crust so a crack is a crack,
		// not a 200-unit stripe. LavaPixels wraps, so the repeats meet without seams.
		material.Set( "g_vTexCoordScale", new Vector2( 10f, 10f ) );
		return material;
	}

	static Material CreateMaterial( string name, float[,] art, string baseMaterial = ArenaGenerator.SurfaceMaterialPath )
	{
		var data = new byte[Size * Size * 4];

		for ( var y = 0; y < Size; y++ )
		{
			for ( var x = 0; x < Size; x++ )
			{
				var v = (byte)(art[x / Upscale, y / Upscale].Clamp( 0f, 1f ) * 255f);
				var i = (y * Size + x) * 4;

				data[i + 0] = v;
				data[i + 1] = v;
				data[i + 2] = v;
				data[i + 3] = 255;
			}
		}

		var texture = Texture.Create( Size, Size, ImageFormat.RGBA8888 )
			.WithName( name )
			.WithData( data )
			.Finish();

		// A copy of the matte dev surface rather than a bare Material.Create: the copy keeps every
		// parameter that already works on the tiles (tint amount, roughness, fog) and only the
		// colour texture changes.
		var material = Material.Load( baseMaterial ).CreateCopy( name );
		material.Set( "Color", texture );

		// Full model tint. The dev surface only applies part of it, which let strong hues like
		// teal show through but washed the sand floor out to grey.
		material.Set( "g_flModelTintAmount", 1f );
		return material;
	}

	/// <summary>
	/// Soft candy block: near-flat fill with the faintest grain, corners knocked off so the tile
	/// reads as rounded, a light rim top-left and a soft shadow bottom-right, plus a small specular
	/// "shine" in the top-left quarter. Deterministic: the same seed gives the same block everywhere.
	/// </summary>
	static float[,] BlockPixels( int seed )
	{
		var random = new System.Random( seed );
		var art = new float[ArtSize, ArtSize];
		var last = ArtSize - 1;

		for ( var y = 0; y < ArtSize; y++ )
		{
			for ( var x = 0; x < ArtSize; x++ )
			{
				var v = 0.93f + (float)random.NextDouble() * 0.03f;

				if ( x == 0 || y == 0 ) v = 1.0f;
				else if ( x == last || y == last ) v = 0.70f;
				else if ( x == last - 1 || y == last - 1 ) v = 0.82f;

				// Rounded corners: the four corner pixels and their diagonal neighbours sink into the seam.
				var cornerX = x == 0 || x == last;
				var cornerY = y == 0 || y == last;
				if ( cornerX && cornerY ) v = 0.62f;

				// Shine: a small L of highlight near the top-left, like light on a glossy toy brick.
				if ( (y == 2 && x >= 2 && x <= 5) || (x == 2 && y >= 3 && y <= 4) ) v = 1.0f;

				art[x, y] = v;
			}
		}

		return art;
	}

	/// <summary>
	/// Cream plank: soft horizontal grooves and two round studs, for the frame and posts.
	/// </summary>
	static float[,] PlatePixels()
	{
		var art = new float[ArtSize, ArtSize];

		for ( var y = 0; y < ArtSize; y++ )
		{
			for ( var x = 0; x < ArtSize; x++ )
			{
				var v = 0.92f;

				if ( y == 0 ) v = 1.0f;
				else if ( y == ArtSize / 2 ) v = 0.78f;
				else if ( y == ArtSize - 1 ) v = 0.74f;

				var dx = (x % 8) - 3.5f;
				var dy = (y % 8) - 3.5f;
				if ( dx * dx + dy * dy < 3.2f ) v = dy < 0 ? 1.0f : 0.84f;

				art[x, y] = v;
			}
		}

		return art;
	}

	/// <summary>
	/// Lava crust: scattered cell centres, and each pixel's brightness rises as it nears the border
	/// between its two closest cells — a cheap Voronoi crack pattern. Wraps at the edges so the
	/// slab can tile without seams.
	/// </summary>
	static float[,] LavaPixels( int seed )
	{
		var random = new System.Random( seed );
		var cells = new Vector2[10];

		for ( var i = 0; i < cells.Length; i++ )
			cells[i] = new Vector2( (float)random.NextDouble() * ArtSize, (float)random.NextDouble() * ArtSize );

		var art = new float[ArtSize, ArtSize];

		for ( var y = 0; y < ArtSize; y++ )
		{
			for ( var x = 0; x < ArtSize; x++ )
			{
				var nearest = float.MaxValue;
				var second = float.MaxValue;

				foreach ( var cell in cells )
				{
					// Toroidal distance, so the pattern wraps.
					var dx = MathX.Clamp( x + 0.5f - cell.x, -ArtSize, ArtSize );
					var dy = MathX.Clamp( y + 0.5f - cell.y, -ArtSize, ArtSize );
					if ( dx > ArtSize * 0.5f ) dx -= ArtSize;
					if ( dx < -ArtSize * 0.5f ) dx += ArtSize;
					if ( dy > ArtSize * 0.5f ) dy -= ArtSize;
					if ( dy < -ArtSize * 0.5f ) dy += ArtSize;

					var d = dx * dx + dy * dy;
					if ( d < nearest ) { second = nearest; nearest = d; }
					else if ( d < second ) second = d;
				}

				var edge = MathF.Sqrt( second ) - MathF.Sqrt( nearest );
				art[x, y] = edge < 1.1f ? 1.0f : 0.38f + (float)random.NextDouble() * 0.08f;
			}
		}

		return art;
	}

	/// <summary>Soft candy stripes, four art pixels wide, with the same rounded rim as the block.</summary>
	static float[,] HazardPixels()
	{
		var art = new float[ArtSize, ArtSize];
		var last = ArtSize - 1;

		for ( var y = 0; y < ArtSize; y++ )
		{
			for ( var x = 0; x < ArtSize; x++ )
			{
				var stripe = ((x + y) / 4) % 2 == 0;

				// The light stripe is near-white so the tint shows as cream, the dark one carries the colour.
				var v = stripe ? 1.0f : 0.80f;

				if ( x == last || y == last ) v *= 0.72f;
				else if ( x == 0 || y == 0 ) v = 1.0f;

				if ( (x == 0 || x == last) && (y == 0 || y == last) ) v = 0.62f;

				art[x, y] = v;
			}
		}

		return art;
	}
}
