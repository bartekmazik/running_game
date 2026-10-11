using System;

/// <summary>
/// The lava's surface: the user's Kenney blob pattern from the project, or a crust generated in
/// code from a fixed seed when the file is missing. Greyscale, so ModelRenderer.Tint carries the
/// lava's colour and its pulse. (Everything else in the arena is painted by <see cref="Painter"/>.)
/// </summary>
public static class TileTextures
{
	/// <summary>Logical pixels per side of the generated crust.</summary>
	const int ArtSize = 16;

	/// <summary>Texture pixels per logical pixel. Upscaling in code keeps the pixels crisp.</summary>
	const int Upscale = 8;

	const int Size = ArtSize * Upscale;

	// Deliberately no static cache. s&box hotload keeps static fields alive across code reloads and
	// play sessions, so a cached material outlives the code that built it — every texture change
	// silently kept showing the first version. The caller builds these once per arena and holds them.

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
}
