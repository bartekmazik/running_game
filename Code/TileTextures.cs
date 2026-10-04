using System;

/// <summary>
/// Tile surfaces generated in code at startup — no texture files. CLAUDE.md keeps the project to
/// built-in assets and code-generated content; a texture computed from a fixed seed is the same
/// kind of thing as the arena itself, just in pixels.
///
/// Every texture is greyscale (or near it) on purpose: the floor's identity colour still comes
/// from ModelRenderer.Tint, so one block texture serves all five floors.
///
/// The look is a 16x16 "pixel" block, upscaled with hard edges, in the spirit of the game this
/// is modelled on: grainy fill, a light bevel on the top-left rim and a dark one bottom-right, so
/// every tile reads as a separate block even on a single-colour floor.
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

	/// <summary>The ordinary floor block.</summary>
	public static Material CreateBlock() => CreateMaterial( "tnt_block", BlockPixels( 1337 ) );

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
		var material = CreateMaterial( "tnt_lava", LavaPixels( 4242 ), baseMaterial );

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
	/// Grainy block fill with a bevelled rim. Deterministic: the same seed gives the same block on
	/// every machine, so every player sees the same floor.
	/// </summary>
	static float[,] BlockPixels( int seed )
	{
		var random = new System.Random( seed );
		var art = new float[ArtSize, ArtSize];

		for ( var y = 0; y < ArtSize; y++ )
		{
			for ( var x = 0; x < ArtSize; x++ )
			{
				// Mostly fine grain, with the odd darker fleck — sand, not static.
				var grain = (float)random.NextDouble() * 0.10f;
				var fleck = random.Next( 9 ) == 0 ? -0.12f : 0f;
				var v = 0.80f + grain + fleck;

				// Bevel: a light rim top and left, a dark rim bottom and right, two pixels deep at
				// the outer edge and one inner step, like a pressed block.
				if ( x == 0 || y == 0 ) v = 1.0f;
				else if ( x == ArtSize - 1 || y == ArtSize - 1 ) v = 0.50f;
				else if ( x == 1 || y == 1 ) v += 0.06f;
				else if ( x == ArtSize - 2 || y == ArtSize - 2 ) v -= 0.10f;

				art[x, y] = v;
			}
		}

		return art;
	}

	/// <summary>
	/// Steel plate: a seam across the middle, a rivet in each quarter, faint vertical brushing.
	/// </summary>
	static float[,] PlatePixels()
	{
		var art = new float[ArtSize, ArtSize];

		for ( var y = 0; y < ArtSize; y++ )
		{
			for ( var x = 0; x < ArtSize; x++ )
			{
				var v = 0.62f + (x % 3 == 0 ? 0.04f : 0f);

				if ( y == ArtSize / 2 || y == 0 ) v = 0.34f;
				else if ( y == ArtSize / 2 + 1 || y == 1 ) v = 0.78f;

				var rivet = (x == 3 || x == 12) && (y == 4 || y == 12);
				if ( rivet ) v = 0.95f;

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

	/// <summary>Bold diagonal stripes, four art pixels wide, with the same bevelled rim.</summary>
	static float[,] HazardPixels()
	{
		var art = new float[ArtSize, ArtSize];

		for ( var y = 0; y < ArtSize; y++ )
		{
			for ( var x = 0; x < ArtSize; x++ )
			{
				var stripe = ((x + y) / 4) % 2 == 0;
				var v = stripe ? 1.0f : 0.14f;

				if ( x == ArtSize - 1 || y == ArtSize - 1 ) v *= 0.55f;
				else if ( x == 0 || y == 0 ) v = v * 0.8f + 0.2f;

				art[x, y] = v;
			}
		}

		return art;
	}
}
